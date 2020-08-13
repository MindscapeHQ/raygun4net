using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Logging;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Storage;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    [ThreadStatic]
    private static RaygunRequestMessage _currentRequestMessage;
    private static object _sendLock = new object();

    private readonly string _apiKey;
    private readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private IRaygunOfflineStorage _offlineStorage = new RaygunIsolatedStorage();

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));
      _wrapperExceptions.Add(typeof(HttpUnhandledException));

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreFormFieldNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreFormFieldNames.Split(',');
        IgnoreFormFieldNames(ignoredNames);
      }

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreHeaderNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreHeaderNames.Split(',');
        IgnoreHeaderNames(ignoredNames);
      }

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreCookieNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreCookieNames.Split(',');
        IgnoreCookieNames(ignoredNames);
      }

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreServerVariableNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreServerVariableNames.Split(',');
        IgnoreServerVariableNames(ignoredNames);
      }

      IsRawDataIgnored = RaygunSettings.Settings.IsRawDataIgnored;

      RaygunLogger.Instance.LogLevel = RaygunSettings.Settings.LogLevel;

      ThreadPool.QueueUserWorkItem(state => { SendStoredMessages(); });
    }

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException or HttpUnhandledException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. TargetInvocationException and HttpUnhandledException will be stripped by default.
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

    #region Message Scrubbing Properties

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

    #endregion // Message Scrubbing Properties

    #region Message Send Methods

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public override void Send(Exception exception)
    {
      Send(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      _currentRequestMessage = BuildRequestMessage();

      Send(BuildMessage(exception, tags, userCustomData, null));
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void SendInBackground(Exception exception)
    {
      SendInBackground(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void SendInBackground(Exception exception, IList<string> tags)
    {
      SendInBackground(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      // We need to process the HttpRequestMessage on the current thread,
      // otherwise it will be disposed while we are using it on the other thread.
      RaygunRequestMessage currentRequestMessage = BuildRequestMessage();

      DateTime? currentTime = DateTime.UtcNow;
      ThreadPool.QueueUserWorkItem(c => {
        _currentRequestMessage = currentRequestMessage;
        Send(BuildMessage(exception, tags, userCustomData, currentTime));
      });
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void SendInBackground(RaygunMessage raygunMessage)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage));
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public override void Send(RaygunMessage raygunMessage)
    {
      if (!ValidateApiKey())
      {
        RaygunLogger.Instance.Warning("Failed to send error report due to invalid API key");
      }

      bool canSend = OnSendingMessage(raygunMessage);

      if (!canSend)
      {
        return;
      }

      string message = null;

      try
      {
        message = SimpleJson.SerializeObject(raygunMessage);
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Failed to serialize report due to: {ex.Message}");

        if (RaygunSettings.Settings.ThrowOnError)
        {
          throw;
        }
      }

      if (string.IsNullOrEmpty(message))
      {
        return;
      }

      bool successfullySentReport = true;

      try
      {
        Send(message);
      }
      catch (Exception ex)
      {
        successfullySentReport = false;

        RaygunLogger.Instance.Error($"Failed to send report to Raygun due to: {ex.Message}");

        SaveMessage(message);

        if (RaygunSettings.Settings.ThrowOnError)
        {
          throw;
        }
      }

      if (successfullySentReport)
      {
        SendStoredMessages();
      }
    }

    private void Send(string message)
    {
      RaygunLogger.Instance.Verbose("Sending Payload --------------");
      RaygunLogger.Instance.Verbose(message);
      RaygunLogger.Instance.Verbose("------------------------------");

      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);
        client.Headers.Add("content-type", "application/json; charset=utf-8");
        client.Encoding = System.Text.Encoding.UTF8;

        client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
      }
    }

    #endregion // Message Send Methods

    #region Message Building Methods

    private RaygunRequestMessage BuildRequestMessage()
    {
      RaygunRequestMessage requestMessage = null;
      HttpContext context = HttpContext.Current;
      if (context != null)
      {
        HttpRequest request = null;
        try
        {
          request = context.Request;
        }
        catch (HttpException) { }

        if (request != null)
        {
          requestMessage = RaygunRequestMessageBuilder.Build(request, _requestMessageOptions);
        }
      }

      return requestMessage;
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData, null);
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime? currentTime)
    {
      exception = StripWrapperExceptions(exception);

      var message = RaygunMessageBuilder.New
        .SetHttpDetails(_currentRequestMessage)
        .SetTimeStamp(currentTime)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
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

    private Exception StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Contains(exception.GetType()) && exception.InnerException != null)
      {
        return StripWrapperExceptions(exception.InnerException);
      }

      return exception;
    }

    #endregion // Message Building Methods

    #region Message Offline Storage

    private void SaveMessage(string message)
    {
      if (!RaygunSettings.Settings.CrashReportingOfflineStorageEnabled)
      {
        RaygunLogger.Instance.Warning("Offline storage is disabled, skipping saving report.");
        return;
      }

      if (!ValidateApiKey())
      {
        RaygunLogger.Instance.Warning("Failed to save report due to invalid API key.");
        return;
      }

      // Avoid writing and reading from disk at the same time with `SendStoredMessages`.
      lock (_sendLock)
      {
        try
        {
          if (!_offlineStorage.Store(message, _apiKey, RaygunSettings.Settings.MaxCrashReportsStoredOffline))
          {
            RaygunLogger.Instance.Warning("Failed to save report to offline storage");
          }
        }
        catch (Exception ex)
        {
          RaygunLogger.Instance.Error($"Failed to save report to offline storage due to: {ex.Message}");
        }
      }
    }

    private void SendStoredMessages()
    {
      if (!RaygunSettings.Settings.CrashReportingOfflineStorageEnabled)
      {
        RaygunLogger.Instance.Warning("Offline storage is disabled, skipping sending stored reports.");
        return;
      }

      if (!ValidateApiKey())
      {
        RaygunLogger.Instance.Warning("Failed to send offline reports due to invalid API key.");
        return;
      }

      lock (_sendLock)
      {
        try
        {
          var files = _offlineStorage.FetchAll(_apiKey);

          foreach (var file in files)
          {
            try
            {
              // Send the stored report.
              Send(file.Contents);

              // Remove the stored report from local storage.
              if (_offlineStorage.Remove(file.Name, _apiKey))
              {
                RaygunLogger.Instance.Info("Successfully removed report from offline storage.");
              }
              else
              {
                RaygunLogger.Instance.Warning("Failed to remove report from offline storage.");
              }
            }
            catch (Exception ex)
            {
              RaygunLogger.Instance.Error($"Failed to send stored report to Raygun due to: {ex.Message}");

              // If just one message fails to send, then don't delete the message,
              // and don't attempt sending anymore until later.
              return;
            }
          }
        }
        catch (Exception ex)
        {
          RaygunLogger.Instance.Error($"Failed to send stored report to Raygun due to: {ex.Message}");
        }
      }
    }

    #endregion // Message Offline Storage

    protected override bool CanSend(Exception exception)
    {
      if (RaygunSettings.Settings.ExcludeErrorsFromLocal && HttpContext.Current != null)
      {
        try
        {
          if (HttpContext.Current.Request.IsLocal)
          {
            return false;
          }
        }
        catch
        {
          if (RaygunSettings.Settings.ThrowOnError)
          {
            throw;
          }
        }
      }
      return base.CanSend(exception);
    }

    protected bool ValidateApiKey()
    {
      return !string.IsNullOrEmpty(_apiKey);
    }
  }
}
