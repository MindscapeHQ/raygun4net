using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using MonoTouch.UIKit;
using MonoTouch.SystemConfiguration;
using MonoTouch.Foundation;
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using MonoTouch.Security;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;
    private static List<Type> _wrapperExceptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      _wrapperExceptions = new List<Type>();
      _wrapperExceptions.Add(typeof(TargetInvocationException));
      _wrapperExceptions.Add(typeof(AggregateException));

      ThreadPool.QueueUserWorkItem(state => { SendStoredMessages(); });
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
    /// Gets or sets the user identity string.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun.io endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

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
    /// Transmits an exception to Raygun.io synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void Send(Exception exception)
    {
      Send(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
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
    /// This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      Send(BuildMessage(exception, tags, userCustomData));
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
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void SendInBackground(Exception exception, IList<string> tags)
    {
      SendInBackground(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      SendInBackground(BuildMessage(exception, tags, userCustomData));
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

    private string _sessionId;

    public void Log(RaygunEventType eventType)
    {
      if (ValidateApiKey ()) {
        if (eventType == RaygunEventType.SessionStart || _sessionId == null) {
          _sessionId = Guid.NewGuid ().ToString ();
        }

        RaygunEventMessage message = new RaygunEventMessage ();
        message.SessionId = _sessionId;
        message.User = UserInfo ?? (!String.IsNullOrEmpty (User) ? new RaygunIdentifierMessage (User) : null);
        message.Type = GetEventType (eventType);
        message.DeviceId = DeviceId;
        if (!String.IsNullOrWhiteSpace (ApplicationVersion)) {
          message.Version = ApplicationVersion;
        } else if (!String.IsNullOrWhiteSpace (NSBundle.MainBundle.ObjectForInfoDictionary ("CFBundleVersion").ToString ())) {
          message.Version = NSBundle.MainBundle.ObjectForInfoDictionary ("CFBundleVersion").ToString ();
        } else {
          message.Version = "Not supplied";
        }

        string messageStr = null;
        try {
          messageStr = SimpleJson.SerializeObject (message);
        } catch (Exception ex) {
          System.Diagnostics.Debug.WriteLine (string.Format ("Error serializing message: {0}", ex.Message));
          return;
        }

        int count = 0;
        if (!String.IsNullOrWhiteSpace (messageStr)) {
          count = SaveEvent (messageStr, message.Timestamp);
        }

        if (eventType == RaygunEventType.SessionEnd || count >= 5) {
          SendStoredMessages ();
        }
      }
    }

    private string DeviceId
    {
      get
      {
        SecRecord query = new SecRecord(SecKind.GenericPassword);
        query.Service = NSBundle.MainBundle.BundleIdentifier;
        query.Account = "RaygunDeviceID";

        NSData deviceId = SecKeyChain.QueryAsData(query);
        if (deviceId == null)
        {
          string id = Guid.NewGuid().ToString();
          query.ValueData = NSData.FromString(id);
          SecStatusCode code = SecKeyChain.Add(query);
          if (code != SecStatusCode.Success && code != SecStatusCode.DuplicateItem)
          {
            System.Diagnostics.Debug.WriteLine(string.Format ("Could not save device ID. Security status code: {0}", code));
            return null;
          }

          return id;
        }
        else
        {
          return deviceId.ToString();
        }
      }
    }

    private string GetEventType(RaygunEventType eventType)
    {
      switch (eventType) {
      case RaygunEventType.SessionStart:
        return "session_start";
      case RaygunEventType.SessionEnd:
        return "session_end";
      case RaygunEventType.SessionTombstoned:
        return "session_tombstoned";
      case RaygunEventType.SessionResurrected:
        return "session_resurrected";
      default:
        return "unknown";
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
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    public static void Attach(string apiKey)
    {
      Detach();
      _client = new RaygunClient(apiKey);
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    /// <param name="user">An identity string for tracking affected users.</param>
    public static void Attach(string apiKey, string user)
    {
      Detach();
      _client = new RaygunClient(apiKey) { User = user };
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Detaches Raygun from listening to unhandled exceptions and unobserved task exceptions.
    /// </summary>
    public static void Detach()
    {
      AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
    }

    private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
      if (e.Exception != null)
      {
        _client.Send(e.Exception);
      }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception)
      {
        _client.Send(e.ExceptionObject as Exception);
      }
    }

    internal RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      exception = StripWrapperExceptions(exception);

      var message = RaygunMessageBuilder.New
        .SetEnvironmentDetails()
        .SetMachineName(UIDevice.CurrentDevice.Name)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();

      return message;
    }

    private static Exception StripWrapperExceptions(Exception exception)
    {
      if (_wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
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
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        if (HasInternetConnection)
        {
          SendStoredMessages();
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
              System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));
              try
              {
                SaveMessage(SimpleJson.SerializeObject(raygunMessage));
                System.Diagnostics.Debug.WriteLine("Exception has been saved to the device to try again later.");
              }
              catch (Exception e)
              {
                System.Diagnostics.Debug.WriteLine(string.Format("Error saving Exception to device {0}", e.Message));
              }
            }
          }
        }
        else
        {
          try
          {
            var message = SimpleJson.SerializeObject(raygunMessage);
            SaveMessage(message);
          }
          catch (Exception ex)
          {
            System.Diagnostics.Debug.WriteLine(string.Format("Error saving Exception to device {0}", ex.Message));
          }
        }
      }
    }

    private bool SendMessage(string message)
    {
      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);
        client.Encoding = System.Text.Encoding.UTF8;

        try
        {
          client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));
          return false;
        }
      }
      return true;
    }

    private bool SendEvents(string message)
    {
      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);
        client.Encoding = System.Text.Encoding.UTF8;

        try
        {
          client.UploadString(RaygunSettings.Settings.EventsEndpoint, message);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error Logging events to Raygun.io {0}", ex.Message));
          return false;
        }
      }
      return true;
    }

    private bool HasInternetConnection
    {
      get
      {
        using (NetworkReachability reachability = new NetworkReachability("raygun.io"))
        {
          NetworkReachabilityFlags flags;
          if (reachability.TryGetFlags(out flags))
          {
            bool isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;
            bool noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;
            if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
            {
              noConnectionRequired = true;
            }
            return isReachable && noConnectionRequired;
          }
        }
        return false;
      }
    }

    private void SendStoredMessages()
    {
      if (ValidateApiKey() && HasInternetConnection)
      {
        try
        {
          using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
          {
            if (isolatedStorage.DirectoryExists("RaygunIO"))
            {
              List<string> eventFileNames = new List<String>();
              string eventPayload = "{\"EventData\":[";

              string[] fileNames = isolatedStorage.GetFileNames("RaygunIO/*.txt");
              foreach (string name in fileNames)
              {
                IsolatedStorageFileStream isoFileStream = isolatedStorage.OpenFile("RaygunIO/" + name, FileMode.Open);
                using (StreamReader reader = new StreamReader (isoFileStream))
                {
                  string text = reader.ReadToEnd ();

                  if(name.StartsWith("RaygunEvent"))
                  {
                    eventPayload += text + ",";
                    eventFileNames.Add("RaygunIO/" + name);
                  }
                  else
                  {
                    bool success = SendMessage(text);
                    // If just one message fails to send, then don't delete the message, and don't attempt sending anymore until later.
                    if (!success)
                    {
                      return;
                    }
                    System.Diagnostics.Debug.WriteLine("Sent " + name);
                    isolatedStorage.DeleteFile("RaygunIO/" + name);
                  }
                }
              }

              if(eventFileNames.Count > 0)
              {
                eventPayload = eventPayload.Substring (0, eventPayload.Length - 1);
                eventPayload += "]}";
                bool success = SendEvents(eventPayload);
                // If the send fails, don't delete the message:
                if(success)
                {
                  System.Diagnostics.Debug.WriteLine("Sent {0} event messages", eventFileNames.Count);
                  foreach (string name in eventFileNames) {
                    isolatedStorage.DeleteFile(name);
                  }
                }
              }

              if (isolatedStorage.GetFileNames("RaygunIO/*.txt").Length == 0)
              {
                System.Diagnostics.Debug.WriteLine("Successfully sent all pending messages");
                isolatedStorage.DeleteDirectory("RaygunIO");
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

    private int SaveEvent(string message, DateTime timestamp)
    {
      try
      {
        using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
        {
          if (!isolatedStorage.DirectoryExists("RaygunIO"))
          {
            isolatedStorage.CreateDirectory("RaygunIO");
          }

          // Examine existing messages
          string[] fileNames = isolatedStorage.GetFileNames ("RaygunIO/RaygunEvent*.txt");
          int count = fileNames.Length;
          int min = count == 0 ? 0 : int.MaxValue;
          int max = 0;
          foreach(string fileName in fileNames)
          {
            string number = fileName.Substring("RaygunEventMessage".Length).Replace(".txt", "");
            int num;
            if(int.TryParse(number, out num))
            {
              min = Math.Min(min, num);
              max = Math.Max(max, num);
            }
          }

          // Save new message to next consecutive slot
          string name = "RaygunIO/RaygunEventMessage" + (max + 1) + ".txt";
          using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(name, FileMode.OpenOrCreate, FileAccess.Write, isolatedStorage))
          {
            using (StreamWriter writer = new StreamWriter(isoStream, Encoding.Unicode))
            {
              writer.Write(message);
              writer.Flush();
              writer.Close();
              count++;
            }
          }

          // If there are more than 10 messages, delete the oldest one
          if(count > 10 && min > 0)
          {
            string nextFileName = "RaygunIO/RaygunEventMessage" + min + ".txt";
            if (isolatedStorage.FileExists(nextFileName))
            {
              isolatedStorage.DeleteFile(nextFileName);
              count--;
            }
          }

          return count;
        }
      }
      catch(Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
      return 0;
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
          System.Diagnostics.Debug.WriteLine("Saved message: " + "RaygunErrorMessage" + number + ".txt");
          System.Diagnostics.Debug.WriteLine("File Count: " + isolatedStorage.GetFileNames("RaygunIO\\*.txt").Length);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }
  }
}
