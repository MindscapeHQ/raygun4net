using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Phone.Info;
using System.IO.IsolatedStorage;
using System.Text;
using Microsoft.Phone.Net.NetworkInformation;
using Mindscape.Raygun4Net.WindowsPhone;
using System.Reflection;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;
    private static List<Type> _wrapperExceptions;
    private List<string> _ignoredFormNames;

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      _wrapperExceptions = new List<Type>();

      Deployment.Current.Dispatcher.BeginInvoke(SendStoredMessages);
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

    private bool IsCalledFromApplicationUnhandledExceptionHandler()
    {
      StackTrace trace = new StackTrace();
      if (trace.FrameCount > 3)
      {
        StackFrame frame = trace.GetFrame(2);
        ParameterInfo[] parameters = frame.GetMethod().GetParameters();
        if (parameters.Length == 2 && parameters[1].ParameterType == typeof(ApplicationUnhandledExceptionEventArgs))
        {
          return true;
        }
      }
      return false;
    }

    private Assembly _callingAssembly;

    private void SetCallingAssembly(Assembly assembly)
    {
      if (!assembly.Equals(Assembly.GetExecutingAssembly()))
      {
        _callingAssembly = assembly;
      }
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      Send(args, null, null);
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IList<string> tags)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      Send(args, tags, null);
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IDictionary userCustomData)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
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
      SetCallingAssembly(Assembly.GetCallingAssembly());
      if (!(args.ExceptionObject is ExitException))
      {
        bool handled = args.Handled;
        args.Handled = true;
        Send(CreateMessage(args.ExceptionObject, tags, userCustomData), false, !handled);
      }
    }

    /// <summary>
    /// Sends a message ro the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    /// <paramref name="version">The version of the running application.</paramref>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IList<string> tags, IDictionary userCustomData, string version)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      if (!(args.ExceptionObject is ExitException))
      {
        bool handled = args.Handled;
        args.Handled = true;
        RaygunMessage message = CreateMessage(args.ExceptionObject, tags, userCustomData);
        message.Details.Version = version;
        Send(message, false, !handled);
      }
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    public void Send(Exception exception)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(exception, null, null, calledFromUnhandled);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(exception, tags, null, calledFromUnhandled);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(Exception exception, IDictionary userCustomData)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(exception, null, userCustomData, calledFromUnhandled);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(exception, tags, userCustomData, calledFromUnhandled);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    /// <param name="version">The version of the running application.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData, string version)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(exception, tags, userCustomData, version, calledFromUnhandled);
    }

    private void Send(Exception exception, IList<string> tags, IDictionary userCustomData, string version, bool calledFromUnhandled)
    {
      if (!(exception is ExitException))
      {
        exception.Data.Add("Message", exception.Message); // TODO is this needed?
        RaygunMessage message = CreateMessage(exception, tags, userCustomData);
        message.Details.Version = version;
        Send(message, calledFromUnhandled, false);
      }
    }

    private void Send(Exception exception, IList<string> tags, IDictionary userCustomData, bool calledFromUnhandled)
    {
      if (!(exception is ExitException))
      {
        exception.Data.Add("Message", exception.Message); // TODO is this needed?
        Send(CreateMessage(exception, tags, userCustomData), calledFromUnhandled, false);
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
      SetCallingAssembly(Assembly.GetCallingAssembly());
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(raygunMessage, calledFromUnhandled, false);
    }

    private void Send(RaygunMessage raygunMessage, bool wait, bool exit)
    {
      if (ValidateApiKey() && !_exit)
      {
        try
        {
          string message = SimpleJson.SerializeObject(raygunMessage);
          if (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.None)
          {
            SendMessage(message, wait, exit);
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
                  SendMessage(text, false, false);
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

    private bool _exit;

    private void SendMessage(string message, bool wait, bool exit)
    {
      _running = true;
      _exit = exit;

      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(RaygunSettings.Settings.ApiEndpoint);
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
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error Logging Exception to Raygun.io " + ex.Message);
      }

      if (wait)
      {
        Thread.Sleep(3000);
      }
      _running = false;
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
      _running = false;
      if (_exit)
      {
        throw new ExitException();
      }
    }

    private RaygunMessage CreateMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);

      var message = RaygunMessageBuilder.New
          .SetCallingAssembly(_callingAssembly)
          .SetEnvironmentDetails()
          .SetMachineName(deviceName.ToString())
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion()
          .SetUser(User)
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
  }
}
