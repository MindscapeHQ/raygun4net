using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : IRaygunClient
  {
    internal const string SentKey = "AlreadySentByRaygun";
    
    private readonly string _apiKey;
    private readonly RaygunSettings _settings;
    private readonly RaygunBreadcrumbs _breadcrumbs;
    private readonly List<Type> _wrapperExceptions = new List<Type>();
    
    private bool _handlingRecursiveErrorSending;
    private bool _handlingRecursiveGrouping;
    
    // Properties
    
    public string User { get; set; }   
    
    public RaygunIdentifierMessage UserInfo { get; set; }
    
    public string ApplicationVersion { get; set; }
    
    // Events
    
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;
    public event EventHandler<RaygunCustomGroupingKeyEventArgs> CustomGroupingKey;

    // Constructors
    
    public RaygunClient(string apiKey)
      : this(new RaygunSettings { ApiKey = apiKey })
    {
    }

    public RaygunClient(RaygunSettings settings)
    {
      _settings = settings;
      _apiKey = settings.ApiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));
      
      #if RAYGUN_NETCORE_WEB
      _wrapperExceptions.Add(typeof(HttpUnhandledException));
      #endif
      
      if (!string.IsNullOrEmpty(settings.ApplicationVersion))
      {
        ApplicationVersion = settings.ApplicationVersion;
      }
      
      #if RAYGUN_NETCORE_WEB
      var breadcrumbStorage = new RaygunHttpContextBreadcrumbStorage();
      #else
      var breadcrumbStorage = new RaygunInMemoryBreadcrumbStorage();
      #endif

      _breadcrumbs = new RaygunBreadcrumbs(_settings, breadcrumbStorage);
    }
    
    // Methods

    public void RecordBreadcrumb(string message)
    {
      _breadcrumbs.Store(new RaygunBreadcrumb { Message = message });
    }

    public void RecordBreadcrumb(RaygunBreadcrumb crumb)
    {
      _breadcrumbs.Store(crumb);
    }

    public void ClearBreadcrumbs()
    {
      _breadcrumbs.Clear();
    }

    public void AddWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (var wrapper in wrapperExceptions)
      {
        if (!_wrapperExceptions.Contains(wrapper))
        {
          _wrapperExceptions.Add(wrapper);
        }
      }
    }

    public void RemoveWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (var wrapper in wrapperExceptions)
      {
        _wrapperExceptions.Remove(wrapper);
      }
    }
    
    public void Send(Exception exception)
    {
      Send(exception, null, null);
    }
    
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, null);
    }

    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      SendAsync(exception, tags, userCustomData).Wait();
    }
    
    private async Task SendAsync(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        await StripAndSend(exception, tags, userCustomData);
        FlagAsSent(exception);
      }
    }

    public Task SendInBackground(Exception exception)
    {
      return SendInBackground(exception, null, null);
    }

    public Task SendInBackground(Exception exception, IList<string> tags)
    {
      return SendInBackground(exception, tags, null);
    }

    public async Task SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        var task = Task.Run(async () =>
        {
          await StripAndSend(exception, tags, userCustomData);
        });
        
        FlagAsSent(exception);
        
        await task;
      }
    }

    public Task SendInBackground(RaygunMessage raygunMessage)
    {
      return Task.Run(() => Send(raygunMessage));
    }
    
    protected bool CanSend(Exception exception)
    {
      if (!ValidateApiKey())
      {
        return false;
      }

      if (exception == null)
      {
        return false;
      }

      if (exception.Data != null)
      {
        return !exception.Data.Contains(SentKey) || false.Equals(exception.Data[SentKey]);
      }

      return true;
    }
    
    protected void FlagAsSent(Exception exception)
    {
      if (exception?.Data != null)
      {
        try
        {
          Type[] genericTypes = exception.Data.GetType().GetTypeInfo().GenericTypeArguments;
          
          if (genericTypes.Length == 0 || genericTypes[0].GetTypeInfo().IsAssignableFrom(typeof(string)))
          {
            exception.Data[SentKey] = true;
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to flag exception as sent: {ex.Message}");
        }
      }
    }
    
    private async Task StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      foreach (var strippedException in StripWrapperExceptions(exception))
      {
        await Send(await BuildMessage(strippedException, tags, userCustomData));
      }
    }
    
    protected IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        if (exception is AggregateException aggregate)
        {
          foreach (var innerException in aggregate.InnerExceptions)
          {
            foreach (var strippedException in StripWrapperExceptions(innerException))
            {
              yield return strippedException;
            }
          }
        }
        else
        {
          foreach (var strippedException in StripWrapperExceptions(exception.InnerException))
          {
            yield return strippedException;
          }
        }
      }
      else
      {
        yield return exception;
      }
    }
    
    protected async Task<RaygunMessage> BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var user = UserInfo ?? (!string.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null);
      
      var message = RaygunMessageBuilder.New(_settings)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(user)
        .SetBreadcrumbs(_breadcrumbs.ToList())
        .Build();

      var customGroupingKey = await OnCustomGroupingKey(exception, message);
      
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }
    
    private async Task<string> OnCustomGroupingKey(Exception exception, RaygunMessage message)
    {
      string result = null;
      
      if (!_handlingRecursiveGrouping)
      {
        var handler = CustomGroupingKey;
        
        if (handler != null)
        {
          var args = new RaygunCustomGroupingKeyEventArgs(exception, message);
          
          try
          {
            handler(this, args);
          }
          catch (Exception e)
          {
            _handlingRecursiveGrouping = true;
            
            await SendAsync(e, null, null);
            
            _handlingRecursiveGrouping = false;
          }
          
          result = args.CustomGroupingKey;
        }
      }
      
      return result;
    }

    public async Task Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingMessage(raygunMessage);
        
        if (canSend)
        {
          using (var client = new HttpClient())
          {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint);

            requestMessage.Headers.Add("X-ApiKey", _apiKey);

            try
            {
              var message = SimpleJson.SerializeObject(raygunMessage);
              
              requestMessage.Content = new StringContent(message, Encoding.UTF8, "application/json");
              
              var result = await client.SendAsync(requestMessage);
              
              if (!result.IsSuccessStatusCode)
              {
                Debug.WriteLine($"Error Logging Exception to Raygun {result.ReasonPhrase}");

                if (_settings.ThrowOnError)
                {
                  throw new Exception("Could not log to Raygun");
                }
              }
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"Error Logging Exception to Raygun {ex.Message}");

              if (_settings.ThrowOnError)
              {
                throw;
              }
            }
          }
        }
      }
    }
    
    protected bool ValidateApiKey()
    {
      if (!string.IsNullOrEmpty(_apiKey))
      {
        return true;
      }

      Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
      
      return false;
    }
    
    // Returns true if the message can be sent, false if the sending is canceled.
    protected bool OnSendingMessage(RaygunMessage raygunMessage)
    {
      bool result = true;

      if (!_handlingRecursiveErrorSending)
      {
        EventHandler<RaygunSendingMessageEventArgs> handler = SendingMessage;
        
        if (handler != null)
        {
          RaygunSendingMessageEventArgs args = new RaygunSendingMessageEventArgs(raygunMessage);
          
          try
          {
            handler(this, args);
          }
          catch (Exception e)
          {
            // Catch and send exceptions that occur in the SendingMessage event handler.
            // Set the _handlingRecursiveErrorSending flag to prevent infinite errors.
            _handlingRecursiveErrorSending = true;
            
            Send(e);
            
            _handlingRecursiveErrorSending = false;
          }
          
          result = !args.Cancel;
        }
      }

      return result;
    }
  }
}
