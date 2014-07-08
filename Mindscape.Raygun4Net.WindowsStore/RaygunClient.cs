using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

using Windows.UI.Xaml;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using Mindscape.Raygun4Net.WindowsPhone;
using System.Reflection;
using System.Net.NetworkInformation;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking;
using Windows.ApplicationModel.Background;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;
    private readonly Queue<string> _messageQueue = new Queue<string>();
    private bool _exit;
    private bool _running;
    private static List<Type> _wrapperExceptions;
    private string _version;

    private string PackageVersion
    {
      get
      {
        if (_version == null)
        {
          var v = Windows.ApplicationModel.Package.Current.Id.Version;

          _version = string.Format("{0}.{1}.{2}.{3}", v.Major.ToString(), v.Minor.ToString(), v.Build.ToString(), v.Revision.ToString());
        }

        return _version;
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      _wrapperExceptions = new List<Type>();
      _wrapperExceptions.Add(typeof(TargetInvocationException));

      BeginSendStoredMessages();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }

    private async void BeginSendStoredMessages()
    {
      await SendStoredMessages();
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
    /// Gets or sets richer data about the currently logged-in user
    /// </summary>
    public RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun.io endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. TargetInvocationException is added for you,
    /// but if you have other wrapper exceptions that you want stripped you can pass them in here.
    /// </summary>
    /// <param name="wrapperExceptions">An enumerable list of exception types that you want removed and replaced with their inner exception.</param>
    public void AddWrapperExceptions(IEnumerable<Type> wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        if (!_wrapperExceptions.Contains(wrapper))
        {
          _wrapperExceptions.Add(wrapper);
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

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    public static void Attach(string apiKey)
    {
      Detach();
      _client = new RaygunClient(apiKey);

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

    private static async void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.Exception is Exception)
      {
        await _client.SendAsync(e.Exception);
      }
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="UnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="UnhandledExceptionEventArgs"/> containing the exception information.</param>
    public async Task SendAsync(UnhandledExceptionEventArgs args)
    {
      await SendAsync(args, null, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="UnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="UnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public async Task SendAsync(UnhandledExceptionEventArgs args, IList<string> tags)
    {
      await SendAsync(args, tags, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="UnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="UnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public async Task SendAsync(UnhandledExceptionEventArgs args, IDictionary userCustomData)
    {
      await SendAsync(args, null, userCustomData);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="UnhandledExceptionEventArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="UnhandledExceptionEventArgs"/> containing the exception information.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public async Task SendAsync(UnhandledExceptionEventArgs args, IList<string> tags, IDictionary userCustomData)
    {
      // Throwing a dummy exception then picking out the InnerException to build/send is a workaround to deal with
      // the fact that the StackTrace on UnhandledExceptionEventArgs.Exception goes null when accessed/inspected.
      // This preserves the actual class, message & trace of the real exception that reached the exception handler
      Exception originalException;

      try
      {
        throw new Exception("", args.Exception);
      }
      catch (Exception e)
      {
        originalException = e;
      }

      bool handled = args.Handled;
      await SendOrSave(BuildMessage(originalException.InnerException, tags, userCustomData), false).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>. The app should not be crashing when this is called.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    public async Task SendAsync(Exception exception)
    {
      await SendAsync(exception, null, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>. The app should not be crashing when this is called.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public async Task SendAsync(Exception exception, IList<string> tags)
    {
      await SendAsync(exception, tags, null);
    }

    /// <summary>
    /// Sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>. The app should not be crashing when this is called..
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public async Task SendAsync(Exception exception, IDictionary userCustomData)
    {
      await SendAsync(exception, null, userCustomData);
    }

    /// <summary>
    /// Sends a message immediately to the Raygun.io endpoint based on the given <see cref="Exception"/>. The app should not be crashing when this is called.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public async Task SendAsync(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      await SendOrSave(BuildMessage(exception, tags, userCustomData), true);
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint. The app should not be crashing when this is called.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void SendAsync(RaygunMessage raygunMessage)
    {
      SendAsync(raygunMessage);
    }

    private bool InternetAvailable()
    {
      IEnumerable<ConnectionProfile> connections = NetworkInformation.GetConnectionProfiles();
      var internetProfile = NetworkInformation.GetInternetConnectionProfile();

      bool internetAvailable = connections != null && connections.Any(c =>
        c.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess) ||
        (internetProfile != null && internetProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
      return internetAvailable;
    }

    private async Task SendOrSave(RaygunMessage raygunMessage, bool attemptSend)
    {
      if (ValidateApiKey() && !_exit)
      {
        try
        {
          string message = SimpleJson.SerializeObject(raygunMessage);

          if (InternetAvailable() && attemptSend)
          {
            await SendMessageAsync(message);
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

    private bool _saveOnFail = true;

    private async Task SendStoredMessages()
    {
      if (InternetAvailable())
      {
        _saveOnFail = false;
        try
        {
          var tempFolder = ApplicationData.Current.TemporaryFolder;

          var raygunFolder = await tempFolder.CreateFolderAsync("RaygunIO", CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

          var files = await raygunFolder.GetFilesAsync().AsTask().ConfigureAwait(false);

          foreach (var file in files)
          {
            string text = await FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false);
            await SendMessageAsync(text).ConfigureAwait(false);

            await file.DeleteAsync().AsTask().ConfigureAwait(false);
          }

          await raygunFolder.DeleteAsync().AsTask().ConfigureAwait(false);
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

    private async Task SendMessageAsync(string message)
    {
      _running = true;

      var httpClient = new HttpClient();

      var request = new HttpRequestMessage(HttpMethod.Post, RaygunSettings.Settings.ApiEndpoint);
      request.Headers.Add("X-ApiKey", _apiKey);
      request.Content = new HttpStringContent(message, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

      _messageQueue.Enqueue(message);
      _running = true;

      try
      {
        _running = true;
        var response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask().ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error Logging Exception to Raygun.io " + ex.Message);
      }

      _running = false;
    }

    private async void SaveMessage(string message)
    {
      try
      {
        var tempFolder = ApplicationData.Current.TemporaryFolder;

        var raygunFolder = await tempFolder.CreateFolderAsync("RaygunIO", CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

        int number = 1;
        while (true)
        {
          bool exists;

          try
          {
            await raygunFolder.GetFileAsync("RaygunErrorMessage" + number + ".txt").AsTask().ConfigureAwait(false);
            exists = true;
          }
          catch (FileNotFoundException) {
            exists = false;
          }
          
          if (!exists)
          {
            string nextFileName = "RaygunErrorMessage" + (number + 1) + ".txt";

            StorageFile nextFile = null;
            try
            {
              nextFile = await raygunFolder.GetFileAsync(nextFileName).AsTask().ConfigureAwait(false);

              await nextFile.DeleteAsync().AsTask().ConfigureAwait(false);
            }
            catch (FileNotFoundException) { }

            break;
          }

          number++;
        }

        if (number == 11)
        {
          try
          {
            StorageFile firstFile = await raygunFolder.GetFileAsync("RaygunErrorMessage1.txt").AsTask().ConfigureAwait(false);
            await firstFile.DeleteAsync().AsTask().ConfigureAwait(false);
          }
          catch (FileNotFoundException) { }
        }

        var file = await raygunFolder.CreateFileAsync("RaygunErrorMessage" + number + ".txt").AsTask().ConfigureAwait(false);
        await FileIO.WriteTextAsync(file, message).AsTask().ConfigureAwait(false);

        Debug.WriteLine("Saved message: " + "RaygunIO\\RaygunErrorMessage" + number + ".txt");
      }
      catch (Exception ex)
      {
        Debug.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }

    private RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      exception = StripWrapperExceptions(exception);

      string deviceName = string.Empty;
      var hostName = NetworkInformation.GetHostNames().FirstOrDefault(h => h.Type == HostNameType.DomainName);

      if (hostName != null)
      {
        deviceName = hostName.CanonicalName;
      }

      string version = PackageVersion;
      if (!String.IsNullOrWhiteSpace(ApplicationVersion))
      {
        version = ApplicationVersion;
      }

      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(deviceName)
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion(version)
          .SetTags(tags)
          .SetUserCustomData(userCustomData)
          .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ?  new RaygunIdentifierMessage(User) : null))
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
  }
}