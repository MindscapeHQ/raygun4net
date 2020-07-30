﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Mindscape.Raygun4Net.Messages;

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
    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();

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
        StripAndSend(exception, tags, userCustomData, userInfo, null);
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
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    /// <param name="exception">The original exception that generated the RaygunMessage</param>
    public void SendInBackground(RaygunMessage raygunMessage, Exception exception)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage, exception));
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

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo, DateTime? currentTime)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData, userInfo, currentTime), exception);
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
          System.Diagnostics.Debug.WriteLine(string.Format("Error serializing exception {0}", ex.Message));

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
            System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));

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
          if (!isolatedStorage.DirectoryExists(directoryName))
          {
            isolatedStorage.CreateDirectory(directoryName);
          }

          int number = 1;
          while (true)
          {
            bool exists = isolatedStorage.FileExists(directoryName + "\\RaygunErrorMessage" + number + ".txt");
            if (!exists)
            {
              string nextFileName = directoryName + "\\RaygunErrorMessage" + (number + 1) + ".txt";
              exists = isolatedStorage.FileExists(nextFileName);
              if (exists)
              {
                isolatedStorage.DeleteFile(nextFileName);
              }
              break;
            }
            number++;
          }

          if (number == 11)
          {
            string firstFileName = directoryName + "\\RaygunErrorMessage1.txt";
            if (isolatedStorage.FileExists(firstFileName))
            {
              isolatedStorage.DeleteFile(firstFileName);
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
            if (isolatedStorage.DirectoryExists(directoryName))
            {
              string[] fileNames = isolatedStorage.GetFileNames(directoryName + "\\*.txt");
              foreach (string name in fileNames)
              {
                IsolatedStorageFileStream isoFileStream = isolatedStorage.OpenFile(directoryName + "\\" + name, FileMode.Open);
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
