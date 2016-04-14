using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Messages;
using Microsoft.AspNet.Http;
using System.Net.Http;
using System.Text;
using Mindscape.Raygun4Net.AspNetCore.Builders;

namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunAspNetCoreClient : RaygunClientBase
  {
    private readonly string _apiKey;
    protected readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private readonly ThreadLocal<HttpContext> _currentRequest = new ThreadLocal<HttpContext>(() => null);
    private readonly ThreadLocal<RaygunRequestMessage> _currentRequestMessage = new ThreadLocal<RaygunRequestMessage>(() => null);
    private readonly ThreadLocal<RaygunResponseMessage> _currentResponseMessage = new ThreadLocal<RaygunResponseMessage>(() => null);
    private readonly RaygunSettings _settings;


    /// <summary>
    /// Gets or sets the username/password credentials which are used to authenticate with the system default Proxy server, if one is set
    /// and requires credentials.
    /// </summary>
    public ICredentials ProxyCredentials { get; set; }

    public RaygunAspNetCoreClient(RaygunSettings settings)
    {
      _settings = settings;
      _apiKey = settings.ApiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));

      if (settings.IgnoreFormFieldNames != null)
      {
        var ignoredNames = settings.IgnoreFormFieldNames;
        IgnoreFormFieldNames(ignoredNames);
      }
      if (settings.IgnoreHeaderNames != null)
      {
        var ignoredNames = settings.IgnoreHeaderNames;
        IgnoreHeaderNames(ignoredNames);
      }
      if (settings.IgnoreCookieNames != null)
      {
        var ignoredNames = settings.IgnoreCookieNames;
        IgnoreCookieNames(ignoredNames);
      }
      if (settings.IgnoreServerVariableNames != null)
      {
        var ignoredNames = settings.IgnoreServerVariableNames;
        IgnoreServerVariableNames(ignoredNames);
      }
      if (!string.IsNullOrEmpty(settings.ApplicationVersion))
      {
        ApplicationVersion = settings.ApplicationVersion;
      }
      IsRawDataIgnored = settings.IsRawDataIgnored;
    }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the Form data of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Form on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    /// <param name="names">Keys to be stripped from the copy of the Form NameValueCollection when sending to Raygun.</param>
    public void IgnoreFormFieldNames(params string[] names)
    {
      _requestMessageOptions.AddFormFieldNames(names);
    }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the headers of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Headers on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    /// <param name="names">Keys to be stripped from the copy of the Headers NameValueCollection when sending to Raygun.</param>
    public void IgnoreHeaderNames(params string[] names)
    {
      _requestMessageOptions.AddHeaderNames(names);
    }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the cookies of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Cookies on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    /// <param name="names">Keys to be stripped from the copy of the Cookies NameValueCollection when sending to Raygun.</param>
    public void IgnoreCookieNames(params string[] names)
    {
      _requestMessageOptions.AddCookieNames(names);
    }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the server variables of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the ServerVariables on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    /// <param name="names">Keys to be stripped from the copy of the ServerVariables NameValueCollection when sending to Raygun.</param>
    public void IgnoreServerVariableNames(params string[] names)
    {
      _requestMessageOptions.AddServerVariableNames(names);
    }

    /// <summary>
    /// Specifies whether or not RawData from web requests is ignored when sending reports to Raygun.io.
    /// The default is false which means RawData will be sent to Raygun.io.
    /// </summary>
    public bool IsRawDataIgnored
    {
      get { return _requestMessageOptions.IsRawDataIgnored; }
      set
      {
        _requestMessageOptions.IsRawDataIgnored = value;
      }
    }

    protected bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }
      return true;
    }
    protected bool CanSend(RaygunMessage message)
    {
      if (message != null && message.Details != null && message.Details.Response != null)
      {
        return !_settings.ExcludedStatusCodes.Contains(message.Details.Response.StatusCode);
      }
      return true;
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public async override void Send(Exception exception)
    {
      await Send(exception, null, (IDictionary)null);
    }

    public async Task SendSync(Exception exception)
    {
      await Send(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public async Task Send(Exception exception, IList<string> tags)
    {
      await Send(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public async Task Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        _currentRequestMessage.Value = await BuildRequestMessage();
        _currentResponseMessage.Value = BuildResponseMessage();

        await StripAndSend(exception, tags, userCustomData);
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public Task SendInBackground(Exception exception)
    {
      return SendInBackground(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public Task SendInBackground(Exception exception, IList<string> tags)
    {
      return SendInBackground(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public async Task SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        // We need to process the Request on the current thread,
        // otherwise it will be disposed while we are using it on the other thread.
        RaygunRequestMessage currentRequestMessage = await BuildRequestMessage();
        RaygunResponseMessage currentResponseMessage = BuildResponseMessage();

        var task = Task.Run(async () =>
        {
          _currentRequestMessage.Value = currentRequestMessage;
          _currentResponseMessage.Value = currentResponseMessage;
          await StripAndSend(exception, tags, userCustomData);
        });
        FlagAsSent(exception);
        await task;
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public Task SendInBackground(RaygunMessage raygunMessage)
    {
      return Task.Run(() => Send(raygunMessage));
    }

    internal void FlagExceptionAsSent(Exception exception)
    {
      base.FlagAsSent(exception);
    }

    private async Task<RaygunRequestMessage> BuildRequestMessage()
    {
      var message = _currentRequest.Value != null ? await RaygunAspNetCoreRequestMessageBuilder.Build(_currentRequest.Value, _requestMessageOptions) : null;
      _currentRequest.Value = null;
      return message;
    }

    private RaygunResponseMessage BuildResponseMessage()
    {
      var message = _currentRequest.Value != null ? RaygunAspNetCoreResponseMessageBuilder.Build(_currentRequest.Value) : null;
      _currentRequest.Value = null;
      return message;
    }

    internal RaygunAspNetCoreClient RaygunCurrentRequest(HttpContext request)
    {
      _currentRequest.Value = request;
      return this;
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var message = RaygunAspNetCoreMessageBuilder.New(_settings)
        .SetResponseDetails(_currentResponseMessage.Value)
        .SetRequestDetails(_currentRequestMessage.Value)
        .SetEnvironmentDetails()
#if DNX451
        .SetMachineName(Environment.MachineName)
#endif
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();

      var customGroupingKey = OnCustomGroupingKey(exception, message);
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }

    private async Task StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        await Send(BuildMessage(e, tags, userCustomData));
      }
    }

    protected IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        AggregateException aggregate = exception as AggregateException;
        if (aggregate != null)
        {
          foreach (Exception e in aggregate.InnerExceptions)
          {
            foreach (Exception ex in StripWrapperExceptions(e))
            {
              yield return ex;
            }
          }
        }
        else
        {
          foreach (Exception e in StripWrapperExceptions(exception.InnerException))
          {
            yield return e;
          }
        }
      }
      else
      {
        yield return exception;
      }
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public async Task Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingMessage(raygunMessage) && CanSend(raygunMessage);
        if (canSend)
        {
          using (var client = new HttpClient())
          {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint);

            requestMessage.Headers.Add("X-ApiKey", _apiKey);

            try
            {
              var message = SimpleJson.SerializeObject(raygunMessage);
              requestMessage.Content = new StringContent(message, Encoding.UTF8, "application/json");
              var result = await client.SendAsync(requestMessage);
              if(!result.IsSuccessStatusCode)
              {
                System.Diagnostics.Debug.WriteLine($"Error Logging Exception to Raygun.io {result.ReasonPhrase}");

                if (_settings.ThrowOnError)
                {
                  throw new Exception("Could not log to Raygun");
                }
              }
            }
            catch (Exception ex)
            {
              System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));

              if (_settings.ThrowOnError)
              {
                throw;
              }
            }
          }
        }
      }
    }
  }
}
