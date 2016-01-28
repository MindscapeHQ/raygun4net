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
using System.Reflection;
using System.Net.NetworkInformation;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking;
using Windows.ApplicationModel.Background;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();
    private string _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      
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

    private bool _handlingRecursiveErrorSending;

    // Returns true if the message can be sent, false if the sending is canceled.
    protected bool OnSendingMessage(RaygunMessage raygunMessage)
    {
      bool result = true;

      if (!_handlingRecursiveErrorSending)
      {
        EventHandler<RaygunSendingMessageEventArgs> handler = SendingMessage;
        if (handler != null)
        {
          RaygunSendingMessageEventArgs args = new RaygunSendingMessageEventArgs(raygunMessage);
          try
          {
            handler(this, args);
          }
          catch (Exception e)
          {
            // Catch and send exceptions that occur in the SendingMessage event handler.
            // Set the _handlingRecursiveErrorSending flag to prevent infinite errors.
            _handlingRecursiveErrorSending = true;
            Send(e);
            _handlingRecursiveErrorSending = false;
          }
          result = !args.Cancel;
        }
      }

      return result;
    }

    /// <summary>
    /// Raised before a message is sent. This can be used to add a custom grouping key to a RaygunMessage before sending it to the Raygun service.
    /// </summary>
    public event EventHandler<RaygunCustomGroupingKeyEventArgs> CustomGroupingKey;

    private bool _handlingRecursiveGrouping;
    protected string OnCustomGroupingKey(Exception exception, RaygunMessage message)
    {
      string result = null;
      if (!_handlingRecursiveGrouping)
      {
        var handler = CustomGroupingKey;
        if (handler != null)
        {
          var args = new RaygunCustomGroupingKeyEventArgs(exception, message);
          try
          {
            handler(this, args);
          }
          catch (Exception e)
          {
            _handlingRecursiveGrouping = true;
            Send(e);
            _handlingRecursiveGrouping = false;
          }
          result = args.CustomGroupingKey;
        }
      }
      return result;
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
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;

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

    private static void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      _client.Send(e.Exception);
    }

    /// <summary>
    /// Asynchronously sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    public async Task SendAsync(Exception exception)
    {
      await SendAsync(exception, null, null);
    }

    /// <summary>
    /// Asynchronously sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public async Task SendAsync(Exception exception, IList<string> tags)
    {
      await SendAsync(exception, tags, null);
    }

    /// <summary>
    /// Asynchronously sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public async Task SendAsync(Exception exception, IDictionary userCustomData)
    {
      await SendAsync(exception, null, userCustomData);
    }

    /// <summary>
    /// Asynchronously sends a message to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public async Task SendAsync(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      await StripAndSendAsync(exception, tags, userCustomData);
    }

    /// <summary>
    /// Asynchronously sends a RaygunMessage to the Raygun.io api endpoint.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public async Task SendAsync(RaygunMessage raygunMessage)
    {
      await SendOrSave(raygunMessage);
    }

    /// <summary>
    /// Sends a message immediately to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    public void Send(Exception exception)
    {
      Send(exception, null, null);
    }

    /// <summary>
    /// Sends a message immediately to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, null);
    }

    /// <summary>
    /// Sends a message immediately to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(Exception exception, IDictionary userCustomData)
    {
      Send(exception, null, userCustomData);
    }

    /// <summary>
    /// Sends a message immediately to the Raygun.io endpoint based on the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the message.</param>
    /// <param name="tags">A list of tags to send with the message.</param>
    /// <param name="userCustomData">Custom data to send with the message.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      StripAndSend(exception, tags, userCustomData);
    }

    /// <summary>
    /// Sends a RaygunMessage immediately to the Raygun.io endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunMessage raygunMessage)
    {
      SendOrSave(raygunMessage).Wait(3000);
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

    private async Task SendOrSave(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingMessage(raygunMessage);
        if (canSend)
        {
          try
          {
            string message = SimpleJson.SerializeObject(raygunMessage);

            if (InternetAvailable())
            {
              await SendMessage(message);
            }
            else
            {
              await SaveMessage(message);
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
            await SendMessage(text).ConfigureAwait(false);
            
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

    private async Task SendMessage(string message)
    {
      var httpClient = new HttpClient();

      var request = new HttpRequestMessage(HttpMethod.Post, RaygunSettings.Settings.ApiEndpoint);
      request.Headers.Add("X-ApiKey", _apiKey);
      request.Content = new HttpStringContent(message, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

      try
      {
        await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask().ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error Logging Exception to Raygun.io " + ex.Message);
        if (_saveOnFail)
        {
          SaveMessage(message).Wait(3000);
        }
      }
    }

    private async Task SaveMessage(string message)
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

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData, null);
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime? currentTime)
    {
      string version = PackageVersion;
      if (!String.IsNullOrWhiteSpace(ApplicationVersion))
      {
        version = ApplicationVersion;
      }

      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetTimeStamp(currentTime)
          .SetMachineName(new EasClientDeviceInformation().FriendlyName)
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion(version)
          .SetTags(tags)
          .SetUserCustomData(userCustomData)
          .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ?  new RaygunIdentifierMessage(User) : null))
          .Build();

      var customGroupingKey = OnCustomGroupingKey(exception, message);
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }

    private string PackageVersion
    {
      get
      {
        if (_version == null)
        {
          try
          {
            // The try catch block is to get the tests to work.
            var v = Windows.ApplicationModel.Package.Current.Id.Version;
            _version = string.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision);
          }
          catch (Exception) { }
        }

        return _version;
      }
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var currentTime = DateTime.UtcNow;
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData, currentTime));
      }
    }

    private async Task StripAndSendAsync(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var tasks = new List<Task>();
      var currentTime = DateTime.UtcNow;
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        tasks.Add(SendAsync(BuildMessage(e, tags, userCustomData, currentTime)));
      }
      await Task.WhenAll(tasks);
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
  }
}