using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    public static HttpClient Client { get; set; }
    private readonly string _apiKey;
    protected readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private readonly ThreadLocal<HttpContext> _currentHttpContext = new ThreadLocal<HttpContext>(() => null);
    private readonly ThreadLocal<RaygunRequestMessage> _currentRequestMessage = new ThreadLocal<RaygunRequestMessage>(() => null);
    private readonly ThreadLocal<RaygunResponseMessage> _currentResponseMessage = new ThreadLocal<RaygunResponseMessage>(() => null);
    private readonly RaygunSettings _settings;


    protected internal const string SentKey = "AlreadySentByRaygun";

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public virtual string User { get; set; }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public virtual RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

    public RaygunClient(string apiKey)
      : this(new RaygunSettings {ApiKey = apiKey})
    {
    }

    public RaygunClient(RaygunSettings settings, HttpContext context = null)
    {
      _settings = settings;
      _apiKey = settings.ApiKey;
      if (Client == null)
        Client = new HttpClient();

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

      if (context != null)
      {
        SetCurrentContext(context);
      }
    }

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException or HttpUnhandledException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. The above two do not need to be added manually,
    /// but if you have other wrapper exceptions that you want stripped you can pass them in here.
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that you want removed and replaced with their inner exception.</param>
    public void AddWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        if (!_wrapperExceptions.Contains(wrapper))
        {
          _wrapperExceptions.Add(wrapper);
        }
      }
    }

    /// <summary>
    /// Specifies types of wrapper exceptions that Raygun should send rather than stripping out and sending the inner exception.
    /// This can be used to remove the default wrapper exceptions (TargetInvocationException and HttpUnhandledException).
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that should no longer be stripped away.</param>
    public void RemoveWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        _wrapperExceptions.Remove(wrapper);
      }
    }

    protected virtual bool CanSend(Exception exception)
    {
      return exception == null || exception.Data == null || !exception.Data.Contains(SentKey) || false.Equals(exception.Data[SentKey]);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception != null && exception.Data != null)
      {
        try
        {
          Type[] genericTypes = exception.Data.GetType().GetTypeInfo().GenericTypeArguments;
          if (genericTypes.Length == 0 || genericTypes[0].IsAssignableFrom(typeof(string)))
          {
            exception.Data[SentKey] = true;
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine(String.Format("Failed to flag exception as sent: {0}", ex.Message));
        }
      }
    }

    /// <summary>
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;

    private bool _handlingRecursiveErrorSending;

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


    /// <summary>
    /// Raised before a message is sent. This can be used to add a custom grouping key to a RaygunMessage before sending it to the Raygun service.
    /// </summary>
    public event EventHandler<RaygunCustomGroupingKeyEventArgs> CustomGroupingKey;

    private bool _handlingRecursiveGrouping;
    protected async Task<string> OnCustomGroupingKey(Exception exception, RaygunMessage message)
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
            await SendAsync(e, null, null, null);
            _handlingRecursiveGrouping = false;
          }
          result = args.CustomGroupingKey;
        }
      }
      return result;
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
    /// Specifies whether or not RawData from web requests is ignored when sending reports to Raygun.
    /// The default is false which means RawData will be sent to Raygun.
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
        Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
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
    /// Transmits an exception to Raygun synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void Send(Exception exception)
    {
      Send(exception, null, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      Send(exception, tags, userCustomData, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="context">The current web HttpContext</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData, HttpContext context)
    {
      SendAsync(exception, tags, userCustomData, context).Wait();
    }

    private async Task SendAsync(Exception exception, IList<string> tags, IDictionary userCustomData, HttpContext context)
    {
      if (CanSend(exception))
      {
        _currentHttpContext.Value = context;
        _currentRequestMessage.Value = await BuildRequestMessage();
        _currentResponseMessage.Value = BuildResponseMessage();

        await StripAndSend(exception, tags, userCustomData);
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public Task SendInBackground(Exception exception)
    {
      return SendInBackground(exception, null, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public Task SendInBackground(Exception exception, IList<string> tags)
    {
      return SendInBackground(exception, tags, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
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
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public Task SendInBackground(RaygunMessage raygunMessage)
    {
      return Task.Run(() => Send(raygunMessage));
    }

    internal void FlagExceptionAsSent(Exception exception)
    {
      FlagAsSent(exception);
    }

    private async Task<RaygunRequestMessage> BuildRequestMessage()
    {
      var message = _currentHttpContext.Value != null ? await RaygunAspNetCoreRequestMessageBuilder.Build(_currentHttpContext.Value, _requestMessageOptions) : null;
      _currentHttpContext.Value = null;
      return message;
    }

    private RaygunResponseMessage BuildResponseMessage()
    {
      var message = _currentHttpContext.Value != null ? RaygunAspNetCoreResponseMessageBuilder.Build(_currentHttpContext.Value) : null;
      _currentHttpContext.Value = null;
      return message;
    }

    public RaygunClient SetCurrentContext(HttpContext request)
    {
      _currentHttpContext.Value = request;
      return this;
    }

    protected async Task<RaygunMessage> BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var message = RaygunAspNetCoreMessageBuilder.New(_settings)
        .SetResponseDetails(_currentResponseMessage.Value)
        .SetRequestDetails(_currentRequestMessage.Value)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();

      var customGroupingKey = await OnCustomGroupingKey(exception, message);
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
        await Send(await BuildMessage(e, tags, userCustomData));
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
    /// Posts a RaygunMessage to the Raygun api endpoint.
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
          HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint);

          requestMessage.Headers.Add("X-ApiKey", _apiKey);

          try
          {
            var message = SimpleJson.SerializeObject(raygunMessage);
            requestMessage.Content = new StringContent(message, Encoding.UTF8, "application/json");
            var result = await Client.SendAsync(requestMessage);
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
            Debug.WriteLine(string.Format("Error Logging Exception to Raygun {0}", ex.Message));

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
