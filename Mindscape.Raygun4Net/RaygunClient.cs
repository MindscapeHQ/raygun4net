using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;
#if WINRT
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
#elif WINDOWS_PHONE
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Phone.Info;
using System.IO.IsolatedStorage;
using System.Text;
using Microsoft.Phone.Net.NetworkInformation;
#else
using System.Web;
using System.Threading;
using System.Reflection;
#endif

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;

#if WINDOWS_PHONE
      Deployment.Current.Dispatcher.BeginInvoke(SendStoredMessages);
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }
      return true;
    }

#if WINRT
  /// <summary>
  /// Sends the exception from an UnhandledException event to Raygun.io, optionally with a list of tags
  /// for identification.
  /// </summary>
  /// <param name="unhandledExceptionEventArgs">The event args from UnhandledException, containing the thrown exception and its message.</param>
  /// <param name="tags">An optional list of strings to identify the message to be transmitted.</param>
  /// <param name="userCustomData">A key-value collection of custom data that is to be sent along with the message</param>
    public void Send(UnhandledExceptionEventArgs unhandledExceptionEventArgs, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      if (ValidateApiKey())
      {
        var exception = unhandledExceptionEventArgs.Exception;
        exception.Data.Add("Message", unhandledExceptionEventArgs.Message);

        Send(CreateMessage(exception, tags, userCustomData));
      }
    }

    /// <summary>
    /// To be called by Wrap() - little point in allowing users to send exceptions in WinRT
    /// as the object contains little useful information besides the exception name and description
    /// </summary>
    /// <param name="exception">The exception thrown by the wrapped method</param>
    /// <param name="tags">A list of string tags relating to the message to identify it</param>
    /// <param name="userCustomData">A key-value collection of custom data that is to be sent along with the message</param>
    private void Send(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      if (ValidateApiKey())
      {
        Send(CreateMessage(exception, tags, userCustomData));
      }
    }

    public async void Send(RaygunMessage raygunMessage)
    {
      HttpClientHandler handler = new HttpClientHandler {UseDefaultCredentials = true};

      var client = new HttpClient(handler);
      {
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("raygun4net-winrt", "1.0.0"));

        HttpContent httpContent = new StringContent(SimpleJson.SerializeObject(raygunMessage));
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-raygun-message");
        httpContent.Headers.Add("X-ApiKey", _apiKey);

        try
        {
          await PostMessageAsync(client, httpContent, RaygunSettings.Settings.ApiEndpoint);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));

          if (RaygunSettings.Settings.ThrowOnError)
          {
            throw;
          }
        }
      }
    }

    private RaygunMessage CreateMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(NetworkInformation.GetHostNames()[0].DisplayName)
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion()
          .Build();

      if (tags != null)
      {
        message.Details.Tags = tags;
      }
      if (userCustomData != null)
      {
        message.Details.UserCustomData = userCustomData;
      }
      return message;
    }

#pragma warning disable 1998
    private async Task PostMessageAsync(HttpClient client, HttpContent httpContent, Uri uri)
#pragma warning restore 1998
    {
      HttpResponseMessage response;
      response = client.PostAsync(uri, httpContent).Result;
      client.Dispose();
    }

    public void Wrap(Action func, [Optional] List<string> tags)
    {
      try
      {
        func();
      }
      catch (Exception ex)
      {
        Send(ex);
        throw;
      }
    }

    public TResult Wrap<TResult>(Func<TResult> func, [Optional] List<string> tags)
    {
      try
      {
        return func();
      }
      catch (Exception ex)
      {
        Send(ex);
        throw;
      }
    }
#elif WINDOWS_PHONE

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args)
    {
      Send(args, null, null);
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IList<string> tags)
    {
      Send(args, tags, null);
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IDictionary userCustomData)
    {
      Send(args, null, userCustomData);
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IList<string> tags, IDictionary userCustomData)
    {
      Send(CreateMessage(args.ExceptionObject, tags, userCustomData));
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    public void Send(Exception exception)
    {
      Send(exception, null, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(Exception exception, IDictionary userCustomData)
    {
      Send(exception, null, userCustomData);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (ValidateApiKey())
      {
        exception.Data.Add("Message", exception.Message);

        Send(CreateMessage(exception, tags, userCustomData));
      }
    }

    private bool _running;

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        try
        {
          string message = SimpleJson.SerializeObject(raygunMessage);
          if (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.None)
          {
            SendMessage(message, true);
          }
          else
          {
            SaveMessage(message);
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));
        }
      }
    }

    private void SendStoredMessages()
    {
      if (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.None)
      {
        try
        {
          using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
          {
            if (isolatedStorage.DirectoryExists("RaygunIO"))
            {
              string[] fileNames = isolatedStorage.GetFileNames("RaygunIO\\*.txt");
              foreach (string name in fileNames)
              {
                IsolatedStorageFileStream isoFileStream = isolatedStorage.OpenFile("RaygunIO\\" + name, FileMode.Open);
                using (StreamReader reader = new StreamReader(isoFileStream))
                {
                  string text = reader.ReadToEnd();
                  SendMessage(text, false);
                }
                isolatedStorage.DeleteFile("RaygunIO\\" + name);
              }
              isolatedStorage.DeleteDirectory("RaygunIO");
            }
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine(string.Format("Error sending stored messages to Raygun.io {0}", ex.Message));
        }
      }
    }

    private readonly Queue<string> _messageQueue = new Queue<string>();

    private void SendMessage(string message, bool wait)
    {
      _running = true;

      var httpWebRequest = (HttpWebRequest)WebRequest.Create(RaygunSettings.Settings.ApiEndpoint);
      httpWebRequest.ContentType = "application/x-raygun-message";
      httpWebRequest.Method = "POST";
      httpWebRequest.Headers["X-Apikey"] = _apiKey;
      httpWebRequest.AllowReadStreamBuffering = false;
      _messageQueue.Enqueue(message);
      _running = true;
      httpWebRequest.BeginGetRequestStream(RequestReady, httpWebRequest);

      while (_running)
      {
        Thread.Sleep(10);
      }

      try
      {
        _running = true;
        httpWebRequest.BeginGetResponse(ResponseReady, httpWebRequest);
        if (wait)
        {
          for (int i = 0; i < 100; i++)
          {
            if (!_running)
            {
              break;
            }
            Thread.Sleep(10);
          }
        }
        _running = false;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error Logging Exception to Raygun.io " + ex.Message);
      }
    }

    private void SaveMessage(string message)
    {
      try
      {
        using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
        {
          if (!isolatedStorage.DirectoryExists("RaygunIO"))
          {
            isolatedStorage.CreateDirectory("RaygunIO");
          }
          int number = 1;
          while (true)
          {
            bool exists = isolatedStorage.FileExists("RaygunIO\\RaygunErrorMessage" + number + ".txt");
            if (!exists)
            {
              string nextFileName = "RaygunIO\\RaygunErrorMessage" + (number + 1) + ".txt";
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
            string firstFileName = "RaygunIO\\RaygunErrorMessage1.txt";
            if (isolatedStorage.FileExists(firstFileName))
            {
              isolatedStorage.DeleteFile(firstFileName);
            }
          }
          using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("RaygunIO\\RaygunErrorMessage" + number + ".txt", FileMode.OpenOrCreate, FileAccess.Write, isolatedStorage))
          {
            using (StreamWriter writer = new StreamWriter(isoStream, Encoding.Unicode))
            {
              writer.Write(message);
              writer.Flush();
              writer.Close();
            }
          }
          Debug.WriteLine("Saved message: " + "RaygunIO\\RaygunErrorMessage" + number + ".txt");
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }

    private void RequestReady(IAsyncResult asyncResult)
    {
      HttpWebRequest request = asyncResult.AsyncState as HttpWebRequest;

      if (request != null)
      {
        using (Stream stream = request.EndGetRequestStream(asyncResult))
        {
          using (StreamWriter writer = new StreamWriter(stream))
          {
            string message = _messageQueue.Dequeue();
            writer.Write(message);
            writer.Flush();
            writer.Close();
          }
        }
      }
      else
      {
        throw new InvalidOperationException("The HttpWebRequest was unexpectedly null.");
      }

      _running = false;
    }

    private void ResponseReady(IAsyncResult asyncResult)
    {
      Deployment.Current.Dispatcher.BeginInvoke(Ready);
    }

    private void Ready()
    {
      _running = false;
    }

    private RaygunMessage CreateMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);

      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(deviceName.ToString())
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion()
          .Build();

      if (tags != null)
      {
        message.Details.Tags = tags;
      }
      if (userCustomData != null)
      {
        message.Details.UserCustomData = userCustomData;
      }
      return message;
    }
#else
    /// <summary>
    /// Transmits an exception to Raygun.io synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    public void Send(Exception exception)
    {
      exception = StripTargetInvocationException(exception);
      Send(BuildMessage(exception));
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    /// <param name="tags">A list of strings associated with the message</param>
    public void Send(Exception exception, IList<string> tags)
    {
      exception = StripTargetInvocationException(exception);
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      Send(message);
    }
    
    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    /// <param name="tags">A list of strings associated with the message</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      exception = StripTargetInvocationException(exception);
      var message = BuildMessage(exception);
      message.Details.Tags = tags;      
      message.Details.UserCustomData = userCustomData;
      Send(message);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This specifies a custom version identification number.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    /// <param name="tags">A list of strings associated with the message</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload</param>
    /// <param name="version">A custom version identifiction, associated with a particular build of your project.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData, string version)
    {
      exception = StripTargetInvocationException(exception);
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      message.Details.UserCustomData = userCustomData;
      message.Details.Version = version;
      Send(message);
    }

    private Exception StripTargetInvocationException(Exception exception)
    {
      if (exception is TargetInvocationException && exception.InnerException != null)
      {
        return exception.InnerException;
      }
      return exception;
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception"></param>
    public void SendInBackground(Exception exception)
    {
      var message = BuildMessage(exception);

      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(Exception exception, List<string> tags)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(Exception exception, List<string> tags, string version)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      message.Details.Version = version;
      ThreadPool.QueueUserWorkItem(c => Send(message));
    }


    internal RaygunMessage BuildMessage(Exception exception)
    {
      var message = RaygunMessageBuilder.New
        .SetHttpDetails(HttpContext.Current)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion()
        .Build();
      return message;
    }

    public void SendInBackground(RaygunMessage raygunMessage)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage));
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        using (var client = new WebClient())
        {
          client.Headers.Add("X-ApiKey", _apiKey);
          client.Encoding = System.Text.Encoding.UTF8;

          try
          {            
            var message = SimpleJson.SerializeObject(raygunMessage);
            client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
          }
          catch (Exception ex)
          {            
            System.Diagnostics.Trace.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));

            if (RaygunSettings.Settings.ThrowOnError)
            {
              throw;
            }
          }
        }
      }
    }
#endif
  }
}
