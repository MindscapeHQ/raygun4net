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
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    internal const string UnhandledExceptionTag = "UnhandledException";

    private readonly string _apiKey;
    private readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    [ThreadStatic]
    private static RaygunRequestMessage _currentRequestMessage;

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

      ThreadPool.QueueUserWorkItem(state => { SendStoredMessages(); });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
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

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public override void Send(Exception exception)
    {
      Send(exception, null, (IDictionary)null, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, (IDictionary)null, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
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
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
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
        _currentRequestMessage = BuildRequestMessage();

        Send(BuildMessage(exception, tags, userCustomData, userInfo, null), exception);
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void SendInBackground(Exception exception)
    {
      SendInBackground(exception, null, (IDictionary)null, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void SendInBackground(Exception exception, IList<string> tags)
    {
      SendInBackground(exception, tags, (IDictionary)null, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      SendInBackground(exception, tags, userCustomData, null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="userInfo">Information about the user including the identity string.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo)
    {
      if (CanSend(exception))
      {
        // We need to process the HttpRequestMessage on the current thread,
        // otherwise it will be disposed while we are using it on the other thread.
        RaygunRequestMessage currentRequestMessage = BuildRequestMessage();
        DateTime currentTime = DateTime.UtcNow;

        ThreadPool.QueueUserWorkItem(c =>
        {
          try
          {
            _currentRequestMessage = currentRequestMessage;
            Send(BuildMessage(exception, tags, userCustomData, userInfo, currentTime), exception);
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
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    /// <param name="exception">The original exception object that the <paramref name="raygunMessage"/> is based upon.</param>
    public void SendInBackground(RaygunMessage raygunMessage, Exception exception = null)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage, exception));
    }

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
          System.Diagnostics.Trace.WriteLine("Error retrieving HttpRequest {0}", ex.Message);
        }

        if (request != null)
        {
          requestMessage = RaygunRequestMessageBuilder.Build(request, _requestMessageOptions);
        }
      }

      return requestMessage;
    }

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
        .SetContextId(ContextId)
        .SetUser(userInfoMessage ?? UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();

      var customGroupingKey = OnCustomGroupingKey(exception, message);
      if(string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }
      return message;
    }

    private Exception StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        return StripWrapperExceptions(exception.InnerException);
      }

      return exception;
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    /// <param name="exception">The original exception that generated the RaygunMessage</param>
    public override void Send(RaygunMessage raygunMessage, Exception exception = null)
    {
      bool canSend = OnSendingMessage(raygunMessage, exception);
      if (canSend)
      {
        string message = null;
        try
        {
          message = SimpleJson.SerializeObject(raygunMessage);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Trace.WriteLine(string.Format("Error serializing exception {0}", ex.Message));

          if (RaygunSettings.Settings.ThrowOnError)
          {
            throw;
          }
        }

        if (message != null)
        {
          try
          {
            Send(message);
          }
          catch (Exception ex)
          {
            SaveMessage(message);
            System.Diagnostics.Trace.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));

            if (RaygunSettings.Settings.ThrowOnError)
            {
              throw;
            }
          }

          SendStoredMessages();
        }
      }
    }

    private void Send(string message)
    {
      if (ValidateApiKey())
      {
        using (var client = CreateWebClient())
        {
          client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
        }
      }
    }

    protected WebClient CreateWebClient()
    {
      var client = new WebClient();
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

    private void SaveMessage(string message)
    {
      try
      {
        using (IsolatedStorageFile isolatedStorage = GetIsolatedStorageScope())
        {
          string directoryName = "RaygunOfflineStorage";
          string[] directories = isolatedStorage.GetDirectoryNames("*");
          if (!FileExists(directories, directoryName))
          {
            isolatedStorage.CreateDirectory(directoryName);
          }

          int number = 1;
          string[] files = isolatedStorage.GetFileNames(directoryName + "\\*.txt");
          while (true)
          {
            bool exists = FileExists(files, "RaygunErrorMessage" + number + ".txt");
            if (!exists)
            {
              string nextFileName = "RaygunErrorMessage" + (number + 1) + ".txt";
              exists = FileExists(files, nextFileName);
              if (exists)
              {
                isolatedStorage.DeleteFile(directoryName + "\\" + nextFileName);
              }
              break;
            }
            number++;
          }

          if (number == 11)
          {
            string firstFileName = "RaygunErrorMessage1.txt";
            if (FileExists(files, firstFileName))
            {
              isolatedStorage.DeleteFile(directoryName + "\\" + firstFileName);
            }
          }
          using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(directoryName + "\\RaygunErrorMessage" + number + ".txt", FileMode.OpenOrCreate, FileAccess.Write, isolatedStorage))
          {
            using (StreamWriter writer = new StreamWriter(isoStream, Encoding.Unicode))
            {
              writer.Write(message);
              writer.Flush();
              writer.Close();
            }
          }
          System.Diagnostics.Trace.WriteLine("Saved message: " + "RaygunErrorMessage" + number + ".txt");
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }

    private bool FileExists(string[] files, string fileName)
    {
      foreach (string str in files)
      {
        if (fileName.Equals(str))
        {
          return true;
        }
      }
      return false;
    }

    private static object _sendLock = new object();

    private void SendStoredMessages()
    {
      lock (_sendLock)
      {
        try
        {
          using (IsolatedStorageFile isolatedStorage = GetIsolatedStorageScope())
          {
            string directoryName = "RaygunOfflineStorage";
            string[] directories = isolatedStorage.GetDirectoryNames("*");
            if (FileExists(directories, directoryName))
            {
              string[] fileNames = isolatedStorage.GetFileNames(directoryName + "\\*.txt");
              foreach (string name in fileNames)
              {
                IsolatedStorageFileStream isoFileStream = new IsolatedStorageFileStream(directoryName + "\\" + name, FileMode.Open, isolatedStorage);
                using (StreamReader reader = new StreamReader(isoFileStream))
                {
                  string text = reader.ReadToEnd();
                  try
                  {
                    Send(text);
                  }
                  catch
                  {
                    // If just one message fails to send, then don't delete the message, and don't attempt sending anymore until later.
                    return;
                  }
                  System.Diagnostics.Debug.WriteLine("Sent " + name);
                }
                isolatedStorage.DeleteFile(directoryName + "\\" + name);
              }
              if (isolatedStorage.GetFileNames(directoryName + "\\*.txt").Length == 0)
              {
                System.Diagnostics.Debug.WriteLine("Successfully sent all pending messages");
                isolatedStorage.DeleteDirectory(directoryName);
              }
            }
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error sending stored messages to Raygun.io {0}", ex.Message));
        }
      }
    }

    private IsolatedStorageFile GetIsolatedStorageScope()
    {
      if (AppDomain.CurrentDomain != null && AppDomain.CurrentDomain.ActivationContext != null)
      {
        return IsolatedStorageFile.GetUserStoreForApplication();
      }
      else
      {
        return IsolatedStorageFile.GetUserStoreForAssembly();
      }
    }
  }
}
