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
  public class RaygunClient : RaygunClientBase
  {
    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();
    private readonly Queue<string> _messageQueue = new Queue<string>();

    private Assembly _callingAssembly;
    private bool _exit;
    private bool _running;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      
      _wrapperExceptions.Add(typeof(TargetInvocationException));

      SetCallingAssembly(Assembly.GetCallingAssembly());
      Deployment.Current.Dispatcher.BeginInvoke(SendStoredMessages);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
      SetCallingAssembly(Assembly.GetCallingAssembly());
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

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. TargetInvocationException is added for you,
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
    /// This can be used to remove the default wrapper exception (TargetInvocationException).
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that should no longer be stripped away.</param>
    public void RemoveWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        _wrapperExceptions.Remove(wrapper);
      }
    }

    private static RaygunClient _client;

    /// <summary>
    /// Gets the <see cref="RaygunClient"/> created by the Attach method.
    /// </summary>
    public static RaygunClient Current
    {
      get { return _client; }
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    public static void Attach(string apiKey)
    {
      Detach();
      _client = new RaygunClient(apiKey);
      _client.SetCallingAssembly(Assembly.GetCallingAssembly());
      if (Application.Current != null)
      {
        Application.Current.UnhandledException += Current_UnhandledException;
      }
    }

    /// <summary>
    /// Detaches Raygun from listening to unhandled exceptions.
    /// </summary>
    public static void Detach()
    {
      if (Application.Current != null)
      {
        Application.Current.UnhandledException -= Current_UnhandledException;
      }
    }

    private static void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception)
      {
        _client.Send(e.ExceptionObject);
      }
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

    private void SetCallingAssembly(Assembly assembly)
    {
      if (!assembly.Equals(Assembly.GetExecutingAssembly()))
      {
        _callingAssembly = assembly;
      }
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args)
    {
      Send(args, null, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IList<string> tags)
    {
      Send(args, tags, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IDictionary userCustomData)
    {
      Send(args, null, userCustomData);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="ApplicationUnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="ApplicationUnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(ApplicationUnhandledExceptionEventArgs args, IList<string> tags, IDictionary userCustomData)
    {
      if (!(args.ExceptionObject is ExitException))
      {
        bool handled = args.Handled;
        args.Handled = true;
        Send(BuildMessage(args.ExceptionObject, tags, userCustomData), false, !handled);
      }
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    public override void Send(Exception exception)
    {
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
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(exception, tags, userCustomData, calledFromUnhandled);
    }

    private void Send(Exception exception, IList<string> tags, IDictionary userCustomData, bool calledFromUnhandled)
    {
      if (!(exception is ExitException))
      {
        Send(BuildMessage(exception, tags, userCustomData), calledFromUnhandled, false);
      }
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunMessage raygunMessage)
    {
      bool calledFromUnhandled = IsCalledFromApplicationUnhandledExceptionHandler();
      Send(raygunMessage, calledFromUnhandled, false);
    }

    private void Send(RaygunMessage raygunMessage, bool wait, bool exit)
    {
      if (ValidateApiKey() && !_exit)
      {
        bool canSend = OnSendingMessage(raygunMessage);
        if (canSend)
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
    }

    private bool _saveOnFail = true;

    private void SendStoredMessages()
    {
      if (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.None)
      {
        _saveOnFail = false;
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
        finally
        {
          _saveOnFail = true;
        }
      }
    }

    private void SendMessage(string message, bool wait, bool exit)
    {
      _running = true;
      _exit = exit;

      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(RaygunSettings.Settings.ApiEndpoint);
      httpWebRequest.ContentType = "application/json; charset=utf-8";
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
      if (_messageQueue.Count > 0)
      {
        string message = _messageQueue.Dequeue();
        if (!String.IsNullOrWhiteSpace(message))
        {
          try
          {
            HttpWebRequest request = asyncResult.AsyncState as HttpWebRequest;

            if (request != null)
            {
              using (Stream stream = request.EndGetRequestStream(asyncResult))
              {
                using (StreamWriter writer = new StreamWriter(stream))
                {
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
          }
          catch (Exception e)
          {
            Debug.WriteLine("Error Logging Exception to Raygun.io " + e.Message);
            if (_saveOnFail)
            {
              SaveMessage(message);
            }
          }
          finally
          {
            _running = false;
          }
        }
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

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      exception = StripWrapperExceptions(exception);

      object deviceName;
      DeviceExtendedProperties.TryGetValue("DeviceName", out deviceName);
      
      string version = _callingAssembly != null ? new AssemblyName(_callingAssembly.FullName).Version.ToString() : "Not supplied";
      if (!String.IsNullOrWhiteSpace(ApplicationVersion))
      {
        version = ApplicationVersion;
      }

      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(deviceName.ToString())
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion(version)
          .SetTags(tags)
          .SetUserCustomData(userCustomData)
          .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
          .Build();

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
  }
}
