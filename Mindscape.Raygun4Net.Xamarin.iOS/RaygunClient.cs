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
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;

#if __UNIFIED__
using UIKit;
using SystemConfiguration;
using Foundation;
using Security;
#else
using MonoTouch.UIKit;
using MonoTouch.SystemConfiguration;
using MonoTouch.Foundation;
using MonoTouch.Security;
#endif

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();
    private string _user;
    private RaygunIdentifierMessage _userInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;

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
    public override string User
    {
      get { return _user; }
      set
      {
        _user = value;
        if (_reporter != null && _user != null)
        {
          _reporter.Identify(_user);
        }
      }
    }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public override RaygunIdentifierMessage UserInfo
    {
      get { return _userInfo; }
      set
      {
        _userInfo = value;
        if (_reporter != null)
        {
          string user = _userInfo == null ? "" : UserInfoString (_userInfo);
          if (user.Length != 0) {
            _reporter.Identify(user);
          }
        }
      }
    }

    private static string UserInfoString(RaygunIdentifierMessage userInfo)
    {
      string str = "";
      if (!String.IsNullOrWhiteSpace (userInfo.FullName))
      {
        str += userInfo.FullName + " ";
      }
      else if (!String.IsNullOrWhiteSpace (userInfo.FirstName))
      {
        str += userInfo.FirstName + " ";
      }
      if (!String.IsNullOrWhiteSpace (userInfo.Identifier))
      {
        str += userInfo.Identifier + " ";
      }
      if (!String.IsNullOrWhiteSpace (userInfo.Email))
      {
        str += userInfo.Email + " ";
      }
      if (!String.IsNullOrWhiteSpace (userInfo.UUID))
      {
        str += userInfo.UUID + " ";
      }
      if (str.Length > 0)
      {
        str = str.Substring (0, str.Length - 1); // Removes last space
      }
      return str;
    }

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException or AggregateException,
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
    /// This can be used to remove the default wrapper exceptions (TargetInvocationException and AggregateException).
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
      StripAndSend(exception, tags, userCustomData);
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
      ThreadPool.QueueUserWorkItem(c => StripAndSend(exception, tags, userCustomData));
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

    private string DeviceId
    {
      get
      {
        SecRecord query = new SecRecord (SecKind.GenericPassword);
        query.Service = "Mindscape.Raygun";
        query.Account = "RaygunDeviceID";

        NSData deviceId = SecKeyChain.QueryAsData (query);
        if (deviceId == null)
        {
          string id = Guid.NewGuid ().ToString ();
          query.ValueData = NSData.FromString (id);
          SecStatusCode code = SecKeyChain.Add (query);
          if (code != SecStatusCode.Success && code != SecStatusCode.DuplicateItem)
          {
            System.Diagnostics.Debug.WriteLine (string.Format ("Could not save device ID. Security status code: {0}", code));
            return null;
          }

          return id;
        }
        else
        {
          return deviceId.ToString ();
        }
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

    [DllImport ("libc")]
    private static extern int sigaction (Signal sig, IntPtr act, IntPtr oact);

    enum Signal {
      SIGBUS = 10,
      SIGSEGV = 11
    }

    private const string StackTraceDirectory = "stacktraces";
    private Mindscape.Raygun4Net.Xamarin.iOS.Raygun _reporter;

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    public static void Attach(string apiKey)
    {
      Attach(apiKey, null);
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    /// <param name="canReportNativeErrors">Whether or not to listen to and report native exceptions.</param>
    /// <param name="hijackNativeSignals">When true, this solves the issue where null reference exceptions inside try/catch blocks crash the app, but when false, additional native errors can be reported.</param>
    public static void Attach(string apiKey, bool canReportNativeErrors, bool hijackNativeSignals)
    {
      Attach (apiKey, null, canReportNativeErrors, hijackNativeSignals);
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    /// <param name="user">An identity string for tracking affected users.</param>
    public static void Attach(string apiKey, string user)
    {
      Attach (apiKey, user, false, true);
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    /// <param name="user">An identity string for tracking affected users.</param>
    /// <param name="canReportNativeErrors">Whether or not to listen to and report native exceptions.</param>
    /// <param name="hijackNativeSignals">When true, this solves the issue where null reference exceptions inside try/catch blocks crash the app, but when false, additional native errors can be reported.</param>
    public static void Attach(string apiKey, string user, bool canReportNativeErrors, bool hijackNativeSignals)
    {
      Detach();

      _client = new RaygunClient(apiKey);
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

      if (canReportNativeErrors)
      {
        PopulateCrashReportDirectoryStructure();

        if (hijackNativeSignals)
        {
          IntPtr sigbus = Marshal.AllocHGlobal (512);
          IntPtr sigsegv = Marshal.AllocHGlobal (512);

          // Store Mono SIGSEGV and SIGBUS handlers
          sigaction (Signal.SIGBUS, IntPtr.Zero, sigbus);
          sigaction (Signal.SIGSEGV, IntPtr.Zero, sigsegv);

          _client._reporter = Mindscape.Raygun4Net.Xamarin.iOS.Raygun.SharedReporterWithApiKey (apiKey);

          // Restore Mono SIGSEGV and SIGBUS handlers
          sigaction (Signal.SIGBUS, sigbus, IntPtr.Zero);
          sigaction (Signal.SIGSEGV, sigsegv, IntPtr.Zero);

          Marshal.FreeHGlobal (sigbus);
          Marshal.FreeHGlobal (sigsegv);
        }
        else
        {
          _client._reporter = Mindscape.Raygun4Net.Xamarin.iOS.Raygun.SharedReporterWithApiKey (apiKey);
        }
      }

      _client.User = user; // Set this last so that it can be passed to the native reporter.
      if (user == null && _client._reporter != null)
      {
        _client._reporter.Identify(_client.DeviceId);
      }
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
        if (_client._reporter != null)
        {
          WriteExceptionInformation (_client._reporter.NextReportUUID, e.Exception);
        }
      }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception)
      {
        _client.Send(e.ExceptionObject as Exception);
        if (_client._reporter != null)
        {
          WriteExceptionInformation (_client._reporter.NextReportUUID, e.ExceptionObject as Exception);
        }
      }
    }

    private static string StackTracePath
    {
      get
      {
        string documents = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)[0].Path;
        var path = Path.Combine (documents, "..", "Library", "Caches", StackTraceDirectory);
        return path;
      }
    }

    private static void PopulateCrashReportDirectoryStructure()
    {
      try
      {
        Directory.CreateDirectory(StackTracePath);

        // Write client info to a file to be picked up by the native reporter:
        var clientInfoPath = Path.GetFullPath(Path.Combine(StackTracePath, "RaygunClientInfo"));
        var clientMessage = new RaygunClientMessage();
        string clientInfo = String.Format("{0}\n{1}\n{2}", clientMessage.Version, clientMessage.Name, clientMessage.ClientUrl);
        File.WriteAllText(clientInfoPath, clientInfo);
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine (string.Format ("Failed to populate crash report directory structure: {0}", ex.Message));
      }
    }

    private static void WriteExceptionInformation(string identifier, Exception exception)
    {
      try
      {
        if (exception == null)
        {
          return;
        }

        var path = Path.GetFullPath(Path.Combine(StackTracePath, string.Format ("{0}", identifier)));

        var exceptionType = exception.GetType ();
        string message = exceptionType.Name + ": " + exception.Message;

        File.WriteAllText(path, string.Join(Environment.NewLine, exceptionType.FullName, message, exception.StackTrace));
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine (string.Format ("Failed to write managed exception information: {0}", ex.Message));
      }
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      string machineName = null;
      try
      {
        machineName = UIDevice.CurrentDevice.Name;
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.WriteLine("Exception getting device name {0}", e.Message);
      }

      var message = RaygunMessageBuilder.New
        .SetEnvironmentDetails()
        .SetMachineName(machineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : BuildRaygunIdentifierMessage(machineName)))
        .Build();

      return message;
    }

    private RaygunIdentifierMessage BuildRaygunIdentifierMessage(string machineName)
    {
      string deviceId = DeviceId;
      return !String.IsNullOrWhiteSpace (deviceId) ? new RaygunIdentifierMessage (deviceId) {
        IsAnonymous = true,
        FullName = machineName
      } : null;
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData));
      }
    }

    protected IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        System.AggregateException aggregate = exception as System.AggregateException;
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
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingMessage(raygunMessage);
        if (canSend)
        {
          string message = null;
          try
          {
            message = SimpleJson.SerializeObject(raygunMessage);
          }
          catch (Exception ex)
          {
            System.Diagnostics.Debug.WriteLine (string.Format ("Error serializing message {0}", ex.Message));
          }

          if (message != null)
          {
            try
            {
              SaveMessage (message);
            }
            catch (Exception ex)
            {
              System.Diagnostics.Debug.WriteLine (string.Format ("Error saving Exception to device {0}", ex.Message));
              if (HasInternetConnection)
              {
                SendMessage (message);
              }
            }

            if (HasInternetConnection)
            {
              SendStoredMessages ();
            }
          }
        }
      }
    }

    private bool SendMessage(string message)
    {
      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);
        client.Headers.Add("content-type", "application/json; charset=utf-8");
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
      if (HasInternetConnection)
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
                IsolatedStorageFileStream isoFileStream = isolatedStorage.OpenFile(name, FileMode.Open);
                using (StreamReader reader = new StreamReader(isoFileStream))
                {
                  string text = reader.ReadToEnd();
                  bool success = SendMessage(text);
                  // If just one message fails to send, then don't delete the message, and don't attempt sending anymore until later.
                  if (!success)
                  {
                    return;
                  }
                  System.Diagnostics.Debug.WriteLine("Sent " + name);
                }
                isolatedStorage.DeleteFile(name);
              }
              if (isolatedStorage.GetFileNames("RaygunIO\\*.txt").Length == 0)
              {
                System.Diagnostics.Debug.WriteLine("Successfully sent all pending messages");
              }
              isolatedStorage.DeleteDirectory("RaygunIO");
            }
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error sending stored messages to Raygun.io {0}", ex.Message));
        }
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
