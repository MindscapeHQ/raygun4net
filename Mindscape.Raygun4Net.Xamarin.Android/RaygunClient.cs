using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

using System.Threading;
using System.Reflection;
using Android.Content;
using Android.Views;
using Android.Runtime;
using Android.App;
using Android.Net;
using Java.IO;
using Android.OS;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Provider;

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
      _wrapperExceptions.Add(typeof(System.AggregateException));

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
      if (CanSend(exception))
      {
        Send(BuildMessage(exception, tags, userCustomData));
        FlagAsSent(exception);
      }
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
      if (CanSend(exception))
      {
        ThreadPool.QueueUserWorkItem(c => Send(BuildMessage(exception, tags, userCustomData)));
        FlagAsSent(exception);
      }
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
        try
        {
          return Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine("Failed to get device id: {0}", ex.Message);
        }
        return null;
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
      AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
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
      AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
    }

    /// <summary>
    /// Detaches Raygun from listening to unhandled exceptions and unobserved task exceptions.
    /// </summary>
    public static void Detach()
    {
      AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
      AndroidEnvironment.UnhandledExceptionRaiser -= AndroidEnvironment_UnhandledExceptionRaiser;
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

    private static void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
    {
      if (e.Exception != null)
      {
        _client.Send(e.Exception);
      }
    }

    internal static Context Context
    {
      get { return Application.Context; }
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      exception = StripWrapperExceptions(exception);

      JNIEnv.ExceptionClear();
      var message = RaygunMessageBuilder.New
        .SetEnvironmentDetails()
        .SetMachineName("Unknown")
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : BuildRaygunIdentifierMessage(null)))
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
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingMessage(raygunMessage);
        if (canSend)
        {
          if (HasInternetConnection)
          {
            using (var client = new WebClient())
            {
              client.Headers.Add("X-ApiKey", _apiKey);
              client.Encoding = System.Text.Encoding.UTF8;

              try
              {
                var message = SimpleJson.SerializeObject(raygunMessage);
                client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
                System.Diagnostics.Debug.WriteLine("Sending message to Raygun.io");
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

    private bool HasInternetConnection
    {
      get
      {
        if (Context != null)
        {
          ConnectivityManager connectivityManager = (ConnectivityManager)Context.GetSystemService(Context.ConnectivityService);
          if (connectivityManager != null)
          {
            NetworkInfo networkInfo = connectivityManager.ActiveNetworkInfo;
            return networkInfo != null && networkInfo.IsConnected;
          }
        }
        return false;
      }
    }

    private void SaveMessage(string message)
    {
      try
      {
        if (Context != null)
        {
          using (File dir = Context.GetDir("RaygunIO", FileCreationMode.Private))
          {
            int number = 1;
            string[] files = dir.List();
            while (true)
            {
              bool exists = FileExists(files, "RaygunErrorMessage" + number + ".txt");
              if (!exists)
              {
                string nextFileName = "RaygunErrorMessage" + (number + 1) + ".txt";
                exists = FileExists(files, nextFileName);
                if (exists)
                {
                  DeleteFile(dir, nextFileName);
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
                DeleteFile(dir, firstFileName);
              }
            }

            using (File file = new File(dir, "RaygunErrorMessage" + number + ".txt"))
            {
              using (FileOutputStream stream = new FileOutputStream(file))
              {
                stream.Write(Encoding.ASCII.GetBytes(message));
                stream.Flush();
                stream.Close();
              }
            }
            System.Diagnostics.Debug.WriteLine("Saved message: " + "RaygunErrorMessage" + number + ".txt");
            System.Diagnostics.Debug.WriteLine("File Count: " + dir.List().Length);
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }

    private void SendStoredMessages()
    {
      if (HasInternetConnection)
      {
        try
        {
          using (File dir = Context.GetDir("RaygunIO", FileCreationMode.Private))
          {
            File[] files = dir.ListFiles();
            foreach (File file in files)
            {
              if (file.Name.StartsWith("RaygunErrorMessage"))
              {
                using (FileInputStream stream = new FileInputStream(file))
                {
                  using (InputStreamInvoker isi = new InputStreamInvoker(stream))
                  {
                    using (InputStreamReader streamReader = new Java.IO.InputStreamReader(isi))
                    {
                      using (BufferedReader bufferedReader = new BufferedReader(streamReader))
                      {
                        StringBuilder stringBuilder = new StringBuilder();
                        string line;
                        while ((line = bufferedReader.ReadLine()) != null)
                        {
                          stringBuilder.Append(line);
                        }
                        bool success = SendMessage(stringBuilder.ToString());
                        // If just one message fails to send, then don't delete the message, and don't attempt sending anymore until later.
                        if (!success)
                        {
                          return;
                        }
                        System.Diagnostics.Debug.WriteLine("Sent " + file.Name);
                      }
                    }
                  }
                }
                file.Delete();
              }
            }
            if (dir.List().Length == 0)
            {
              if (files.Length > 0)
              {
                System.Diagnostics.Debug.WriteLine("Successfully sent all pending messages");
              }
              dir.Delete();
            }
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error sending stored messages to Raygun.io {0}", ex.Message));
        }
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

    private void DeleteFile(File dir, string fileName)
    {
      File[] files = dir.ListFiles();
      foreach (File file in files)
      {
        if (fileName.Equals(file.Name))
        {
          file.Delete();
          return;
        }
      }
    }
  }
}
