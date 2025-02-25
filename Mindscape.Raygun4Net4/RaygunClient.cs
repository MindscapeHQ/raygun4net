﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Mindscape.Raygun4Net.Messages;
using System.Web;
using System.Threading;
using System.Reflection;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Breadcrumbs;
using Mindscape.Raygun4Net.Filters;
using Mindscape.Raygun4Net.Logging;
using Mindscape.Raygun4Net.Storage;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    internal const string UnhandledExceptionTag = "UnhandledException";

    private static readonly RaygunBreadcrumbs _breadcrumbs = new RaygunBreadcrumbs(new DefaultBreadcrumbStorage());
    private static object _sendLock = new object();

    private readonly string _apiKey;
    private readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private IRaygunOfflineStorage _offlineStorage = new IsolatedRaygunOfflineStorage();
    private readonly ThrottledBackgroundMessageProcessor _backgroundMessageProcessor;
    private IWebProxy _webProxy;

    /// <summary>
    /// Gets or sets the username/password credentials which are used to authenticate with the system default Proxy server, if one is set
    /// and requires credentials.
    /// </summary>
    public ICredentials ProxyCredentials { get; set; }

    /// <summary>
    /// Gets or sets an IWebProxy instance which can be used to override the default system proxy server settings
    /// </summary>
    public IWebProxy WebProxy
    {
      get => _webProxy;
      set
      {
        _webProxy = value;
        WebClientHelper.WebProxy = _webProxy;
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient() : this(RaygunSettings.Settings.ApiKey) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));

      _wrapperExceptions.Add(typeof(HttpUnhandledException));


      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreSensitiveFieldNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreSensitiveFieldNames.Split(',');
        IgnoreSensitiveFieldNames(ignoredNames);
      }

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreQueryParameterNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreQueryParameterNames.Split(',');
        IgnoreQueryParameterNames(ignoredNames);
      }

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
      IsRawDataIgnoredWhenFilteringFailed = RaygunSettings.Settings.IsRawDataIgnoredWhenFilteringFailed;

      UseXmlRawDataFilter = RaygunSettings.Settings.UseXmlRawDataFilter;
      UseKeyValuePairRawDataFilter = RaygunSettings.Settings.UseKeyValuePairRawDataFilter;

      _backgroundMessageProcessor = new ThrottledBackgroundMessageProcessor(
                                          RaygunSettings.Settings.BackgroundMessageQueueMax,
                                          RaygunSettings.Settings.BackgroundMessageWorkerCount,
                                          RaygunSettings.Settings.BackgroundMessageWorkerBreakpoint,
                                          Send);

      ThreadPool.QueueUserWorkItem(state => { SendStoredMessages(); });
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

    #region Message Scrubbing Properties

    /// <summary>
    /// Adds a list of keys to remove from the following sections of the <see cref="RaygunRequestMessage" />
    /// <see cref="RaygunRequestMessage.Headers" />
    /// <see cref="RaygunRequestMessage.QueryString" />
    /// <see cref="RaygunRequestMessage.Cookies" />
    /// <see cref="RaygunRequestMessage.Data" />
    /// <see cref="RaygunRequestMessage.Form" />
    /// <see cref="RaygunRequestMessage.RawData" />
    /// </summary>
    /// <param name="names">Keys to be stripped from the <see cref="RaygunRequestMessage" />.</param>
    public void IgnoreSensitiveFieldNames(params string[] names)
    {
      _requestMessageOptions.AddSensitiveFieldNames(names);
    }

    /// <summary>
    /// Adds a list of keys to remove from the <see cref="RaygunRequestMessage.QueryString" /> property of the <see cref="RaygunRequestMessage" />
    /// </summary>
    /// <param name="names">Keys to be stripped from the <see cref="RaygunRequestMessage.QueryString" /></param>
    public void IgnoreQueryParameterNames(params string[] names)
    {
      _requestMessageOptions.AddQueryParameterNames(names);
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
      set { _requestMessageOptions.IsRawDataIgnored = value; }
    }

    /// <summary>
    /// Specifies whether or not RawData from web requests is ignored when sensitive values are seen and unable to be removed due to failing to parse the contents.
    /// The default is false which means RawData will not be ignored when filtering fails.
    /// </summary>
    public bool IsRawDataIgnoredWhenFilteringFailed
    {
      get { return _requestMessageOptions.IsRawDataIgnoredWhenFilteringFailed; }
      set { _requestMessageOptions.IsRawDataIgnoredWhenFilteringFailed = value; }
    }

    /// <summary>
    /// Specifies whether or not RawData from web requests is filtered of sensitive values using an XML parser.
    /// </summary>
    /// <value><c>true</c> if use xml raw data filter; otherwise, <c>false</c>.</value>
    public bool UseXmlRawDataFilter
    {
      get { return _requestMessageOptions.UseXmlRawDataFilter; }
      set { _requestMessageOptions.UseXmlRawDataFilter = value; }
    }

    /// <summary>
    /// Specifies whether or not RawData from web requests is filtered of sensitive values using an KeyValuePair parser.
    /// </summary>
    /// <value><c>true</c> if use key pair raw data filter; otherwise, <c>false</c>.</value>
    public bool UseKeyValuePairRawDataFilter
    {
      get { return _requestMessageOptions.UseKeyValuePairRawDataFilter; }
      set { _requestMessageOptions.UseKeyValuePairRawDataFilter = value; }
    }

    /// <summary>
    /// Add an <see cref="IRaygunDataFilter"/> implementation to be used when capturing the raw data
    /// of a HTTP request. This filter will be passed the request raw data and is expected to remove
    /// or replace values whose keys are found in the list supplied to the Filter method.
    /// </summary>
    /// <param name="filter">Custom raw data filter implementation.</param>
    public void AddRawDataFilter(IRaygunDataFilter filter)
    {
      _requestMessageOptions.AddRawDataFilter(filter);
    }

    #endregion // Message Scrubbing Properties

    #region Breadcrumbs

    public static void RecordBreadcrumb(string message)
    {
      _breadcrumbs.Record(new RaygunBreadcrumb { Message = message });
    }

    public static void RecordBreadcrumb(RaygunBreadcrumb crumb)
    {
      _breadcrumbs.Record(crumb);
    }

    public static void ClearBreadcrumbs()
    {
      _breadcrumbs.Clear();
    }

    #endregion // Breadcrumbs

    #region Message Send Methods

    /// <summary>
    /// Transmits an exception to Raygun synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public override void Send(Exception exception)
    {
      Send(exception, null, (IDictionary)null, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, (IDictionary)null, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This uses the version number of the originating assembly.
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
    /// This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="userInfo">Information about the user including the identity string.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo)
    {
      if (CanSend(exception))
      {
        StripAndSend(exception, tags, userCustomData, userInfo, null);
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void SendInBackground(Exception exception)
    {
      SendInBackground(exception, null, (IDictionary)null, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void SendInBackground(Exception exception, IList<string> tags)
    {
      SendInBackground(exception, tags, (IDictionary)null, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      SendInBackground(exception, tags, userCustomData, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="userInfo">Information about the user including the identity string.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo)
    {
      try
      {
        if (CanSend(exception))
        {
          try
          {
            var currentTime = DateTime.UtcNow;

            StripAndSendInBackground(exception, tags, userCustomData, userInfo, currentTime);
          }
          catch (Exception)
          {
            // This will swallow any unhandled exceptions unless we explicitly want to throw on error.
            // Otherwise this can bring the whole process down.
            if (RaygunSettings.Settings.ThrowOnError)
            {
              throw;
            }
          }

          FlagAsSent(exception);
        }
      }
      catch (Exception)
      {
        // This will swallow any unhandled exceptions unless we explicitly want to throw on error.
        // Otherwise this can bring the whole process down.
        if (RaygunSettings.Settings.ThrowOnError)
        {
          throw;
        }
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void SendInBackground(RaygunMessage raygunMessage)
    {
      SendInBackground(() => raygunMessage);
    }

    public void SendInBackground(Func<RaygunMessage> raygunMessage)
    {
      if (!_backgroundMessageProcessor.Enqueue(raygunMessage))
      {
        RaygunLogger.Instance.Debug($"Could not add message to background queue. Dropping message: {raygunMessage}");
      }
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo, DateTime? currentTime)
    {
      var contextId = GetContextId();
      var requestMessage = BuildRequestMessage();
      IList<RaygunBreadcrumb> breadcrumbs = BuildBreadCrumbList();

      foreach (var e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData, userInfo, currentTime, x =>
        {
          x.Details.Request = requestMessage;
          x.Details.Breadcrumbs = breadcrumbs;
          x.Details.ContextId = contextId;
        }));
      }
    }

    private void StripAndSendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo, DateTime? currentTime)
    {
      var contextId = GetContextId();
      var requestMessage = BuildRequestMessage();
      IList<RaygunBreadcrumb> breadcrumbs = BuildBreadCrumbList();

      foreach (var e in StripWrapperExceptions(exception))
      {
        SendInBackground(() => BuildMessage(e, tags, userCustomData, userInfo, currentTime, x =>
        {
          x.Details.Request = requestMessage;
          x.Details.Breadcrumbs = breadcrumbs;
          x.Details.ContextId = contextId;
        }));
      }
    }

    private static IList<RaygunBreadcrumb> BuildBreadCrumbList()
    {
      IList<RaygunBreadcrumb> breadCrumbs = null;
      foreach (var breadCrumb in _breadcrumbs)
      {
        breadCrumbs ??= new List<RaygunBreadcrumb>();
        breadCrumbs.Add(breadCrumb);
      }
      return breadCrumbs ?? Array.Empty<RaygunBreadcrumb>();
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun API endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public override void Send(RaygunMessage raygunMessage)
    {
      if (!ValidateApiKey())
      {
        RaygunLogger.Instance.Warning("Failed to send error report due to invalid API key");
        return;
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

      if (WebProxy != null)
      {
        WebClientHelper.WebProxy = WebProxy;
      }

      WebClientHelper.Send(message, _apiKey, ProxyCredentials);
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
        catch (HttpException ex)
        {
          RaygunLogger.Instance.Error($"Error retrieving HttpRequest {ex.Message}");
        }

        if (request != null)
        {
          requestMessage = RaygunRequestMessageBuilder.Build(request, _requestMessageOptions);
        }
      }

      return requestMessage;
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData = null, RaygunIdentifierMessage userInfoMessage = null, DateTime? currentTime = null, Action<RaygunMessage> customise = null)
    {
      RaygunMessageBuilder builder = RaygunMessageBuilder.New;

      var message = builder
        .SetTimeStamp(currentTime)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(userInfoMessage ?? UserInfo ?? (!string.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Customise(customise)
        .Build();

      var customGroupingKey = OnCustomGroupingKey(exception, message);
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }

    protected IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && (exception.InnerException != null || exception is ReflectionTypeLoadException)))
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
          ReflectionTypeLoadException rtle = exception as ReflectionTypeLoadException;
          if (rtle != null)
          {
            int index = 0;
            foreach (Exception e in rtle.LoaderExceptions)
            {
              try
              {
                e.Data["Type"] = rtle.Types[index];
              }
              catch
              {
              }

              foreach (Exception ex in StripWrapperExceptions(e))
              {
                yield return ex;
              }

              index++;
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
      }
      else
      {
        yield return exception;
      }
    }

    private string GetContextId()
    {
      return HttpContext.Current?.Session?.SessionID;
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
          if (!_offlineStorage.Store(message, _apiKey))
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

    protected bool ValidateApiKey()
    {
      return !string.IsNullOrEmpty(_apiKey);
    }

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
  }
}