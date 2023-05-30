﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Reflection;
using Mindscape.Raygun4Net.Common.DataAccess;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Logging;
using Mindscape.Raygun4Net.Storage;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    private static object _sendLock = new object();

    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private IRaygunOfflineStorage _offlineStorage = new IsolatedRaygunOfflineStorage();

    /// <summary>
    /// Gets or sets the username/password credentials which are used to authenticate with the system default Proxy server, if one is set
    /// and requires credentials.
    /// </summary>
    public ICredentials ProxyCredentials { get; set; }

    /// <summary>
    /// Gets or sets an IWebProxy instance which can be used to override the default system proxy server settings
    /// </summary>
    public IWebProxy WebProxy { get; set; }

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
      DateTime? currentTime = DateTime.UtcNow;
      if (CanSend(exception))
      {
        ThreadPool.QueueUserWorkItem(c =>
        {
          try
          {
            StripAndSend(exception, tags, userCustomData, userInfo, currentTime);
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
        });
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void SendInBackground(RaygunMessage raygunMessage)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage));
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo, DateTime? currentTime)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData, userInfo, currentTime));
      }
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
        RaygunLogger.Instance.Warning("Failed to send error report due to invalid API key.");
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

      using (var client = CreateWebClient())
      {
        client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
      }
    }

    #endregion // Message Send Methods

    #region Message Building Methods

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData, null, null);
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfoMessage)
    {
      return BuildMessage(exception, tags, userCustomData, userInfoMessage, null);
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfoMessage, DateTime? currentTime)
    {
      var message = RaygunMessageBuilder.New
        .SetEnvironmentDetails()
        .SetTimeStamp(currentTime)
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(userInfoMessage ?? UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();
      return message;
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
            RaygunLogger.Instance.Warning("Failed to save report to offline storage.");
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

    protected WebClient CreateWebClient()
    {
      var client = new RaygunWebClient();
      client.Headers.Add("X-ApiKey", _apiKey);
      client.Headers.Add("content-type", "application/json; charset=utf-8");
      client.Encoding = System.Text.Encoding.UTF8;

      if (WebProxy != null)
      {
        client.Proxy = WebProxy;
      }
      else if (WebRequest.DefaultWebProxy != null)
      {
        Uri proxyUri = WebRequest.DefaultWebProxy.GetProxy(new Uri(RaygunSettings.Settings.ApiEndpoint.ToString()));

        if (proxyUri != null && proxyUri.AbsoluteUri != RaygunSettings.Settings.ApiEndpoint.ToString())
        {
          client.Proxy = new WebProxy(proxyUri, false);

          if (ProxyCredentials == null)
          {
            client.UseDefaultCredentials = true;
            client.Proxy.Credentials = CredentialCache.DefaultCredentials;
          }
          else
          {
            client.UseDefaultCredentials = false;
            client.Proxy.Credentials = ProxyCredentials;
          }
        }
      }

      return client;
    }
  }
}
