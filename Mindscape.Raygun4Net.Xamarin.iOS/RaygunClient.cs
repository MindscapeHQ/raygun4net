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
using MonoTouch;
using System.Diagnostics;

#if __UNIFIED__
using UIKit;
using SystemConfiguration;
using Foundation;
using Security;
using ObjCRuntime;
#else
using MonoTouch.UIKit;
using MonoTouch.SystemConfiguration;
using MonoTouch.Foundation;
using MonoTouch.Security;
using MonoTouch.ObjCRuntime;
#endif

using NativeRaygunClient = Mindscape.Raygun4Net.Xamarin.iOS.RaygunClient;
using NativeRaygunUserInfo = Mindscape.Raygun4Net.Xamarin.iOS.RaygunUserInformation;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    private static RaygunClient _client;

    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private string _sessionId;
    private string _deviceId;
    private string _user;
    private RaygunIdentifierMessage _userInfo;
    private PulseEventBatch _activeBatch;
    private static readonly object _batchLock = new object();

    private NativeRaygunClient NativeClient { get; set; }

    /// <summary>
    /// Gets the <see cref="RaygunClient"/> created by the Attach method.
    /// </summary>
    public static RaygunClient Current
    {
      get { return _client; }
    }

    /// <summary>
    /// Gets or sets the maximum number of milliseconds allowed to attempt a synchronous send to Raygun.
    /// A value of 0 will use a timeout of 100 seconds.
    /// The default is 0.
    /// </summary>
    /// <value>The synchronous timeout in milliseconds.</value>
    public int SynchronousTimeout { get; set; }

    private string DeviceId
    {
      get
      {
        if (!string.IsNullOrEmpty(_deviceId))
        {
          return _deviceId;
        }

        try
        {
          string identifier = NSUserDefaults.StandardUserDefaults.StringForKey("io.raygun.identifier");

          if (!String.IsNullOrWhiteSpace(identifier))
          {
            _deviceId = identifier;
            return _deviceId;
          }
        }
        catch
        {
        }

        SecRecord query = new SecRecord(SecKind.GenericPassword);
        query.Service = "Mindscape.Raygun";
        query.Account = "RaygunDeviceID";

        NSData deviceId = SecKeyChain.QueryAsData(query);

        if (deviceId == null)
        {
          // Creating new unique ID
          string id = Guid.NewGuid().ToString();
          query.ValueData = NSData.FromString(id);
          SecStatusCode code = SecKeyChain.Add(query);

          if (code != SecStatusCode.Success && code != SecStatusCode.DuplicateItem)
          {
            Debug.WriteLine(string.Format("Could not save device ID. Security status code: {0}", code));
          }

          _deviceId = id;
        }
        else
        {
          _deviceId = deviceId.ToString();
        }

        return _deviceId;
      }
    }

    private string MachineName
    {
      get
      {
        string machineName = null;
        try
        {
          machineName = UIDevice.CurrentDevice.Name;
        }
        catch (Exception e)
        {
          Debug.WriteLine("Exception getting device name {0}", e.Message);
        }

        return machineName ?? "Unknown";
      }
    }

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public override string User
    {
      get { return _user; }
      set { SetUserInfo(value); }
    }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public override RaygunIdentifierMessage UserInfo
    {
      get { return _userInfo; }
      set { SetUserInfo(value); }
    }

    private void SetUserInfo(string identifier)
    {
      if (string.IsNullOrEmpty(identifier) || string.IsNullOrWhiteSpace(identifier))
      {
        SetUserInfo(GetAnonymousUserInfo());
      }
      else
      {
        SetUserInfo(new RaygunIdentifierMessage(identifier));
      }
    }

    internal void SetUserInfo(RaygunIdentifierMessage userInfo)
    {
      if (_activeBatch != null)
      {
        // Each batch is tied to the UserInfo at the time it's created.
        // So when the user info changes, we end any current batch, so the next one can pick up the new user info.
        _activeBatch.Done();
      }

      // Check info is valid
      if (string.IsNullOrWhiteSpace(userInfo?.Identifier) || string.IsNullOrEmpty(userInfo.Identifier))
      {
        userInfo = GetAnonymousUserInfo();
      }

      // Has the user changed ?
      if (_userInfo != null
       && _userInfo.Identifier != userInfo.Identifier
       && _userInfo.IsAnonymous == false)
      {
        if (!string.IsNullOrEmpty(_sessionId))
        {
          SendPulseSessionEventNow(RaygunPulseSessionEventType.SessionEnd);
          _userInfo = userInfo;
          _user = userInfo.Identifier;
          SendPulseSessionEventNow(RaygunPulseSessionEventType.SessionStart);
        }
      }
      else
      {
        _userInfo = userInfo;
        _user = userInfo.Identifier;
      }

      // Pass on the info to the native Raygun reporter.
      if (NativeClient != null)
      {
        var info = new NativeRaygunUserInfo();
        info.Identifier = _userInfo.Identifier;
        info.IsAnonymous = _userInfo.IsAnonymous;
        info.Email = _userInfo.Email;
        info.FullName = _userInfo.FullName;
        info.FirstName = _userInfo.FirstName;
        NativeClient.UserInformation = info;
      }
    }

    private RaygunIdentifierMessage GetAnonymousUserInfo()
    {
      return new RaygunIdentifierMessage(DeviceId)
      {
        IsAnonymous = true,
        FullName = MachineName,
        UUID = DeviceId
      };
    }

    private string GetVersion()
    {
      string version = ApplicationVersion;
      if (String.IsNullOrWhiteSpace(version))
      {
        try
        {
          string versionNumber = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString").ToString();
          string buildNumber = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString();
          version = String.Format("{0} ({1})", versionNumber, buildNumber);
        }
        catch (Exception ex)
        {
          Trace.WriteLine("Error retieving bundle version {0}", ex.Message);
        }
      }

      if (String.IsNullOrWhiteSpace(version))
      {
        version = "Not supplied";
      }

      return version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;

      // Setting default user information.
      var anonUser = GetAnonymousUserInfo();

      _userInfo = anonUser;
      _user = anonUser.Identifier;

      _wrapperExceptions.Add(typeof(TargetInvocationException));
      _wrapperExceptions.Add(typeof(AggregateException));

      ThreadPool.QueueUserWorkItem(state => { SendStoredMessages(0); });
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }

      return true;
    }

    #region Crash Reporting

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
      StripAndSend(exception, tags, userCustomData, SynchronousTimeout);
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
      ThreadPool.QueueUserWorkItem(c => StripAndSend(exception, tags, userCustomData, 0));
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void SendInBackground(RaygunMessage raygunMessage)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage, 0));
    }

    #endregion

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
    /// <param name="reportNativeErrors">Whether or not to listen to and report native iOS exceptions.</param>
    public static void Attach(string apiKey, bool reportNativeErrors)
    {
      Attach(apiKey, null, reportNativeErrors);
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    /// <param name="user">An identity string for tracking affected users.</param>
    public static void Attach(string apiKey, string user)
    {
      Attach(apiKey, user, false);
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="apiKey">Your app api key.</param>
    /// <param name="user">An identity string for tracking affected users.</param>
    /// <param name="reportNativeErrors">Whether or not to listen to and report native exceptions.</param>
    public static void Attach(string apiKey, string user, bool reportNativeErrors)
    {
      Detach();

      if (_client == null)
      {
        _client = new RaygunClient(apiKey);
      }

      _client.AttachCrashReporting(reportNativeErrors);

      _client.SetUserInfo(user);
    }

    /// <summary>
    /// Initializes the static RaygunClient with the given Raygun api key.
    /// </summary>
    /// <param name="apiKey">Your Raygun api key for this application.</param>
    /// <returns>The RaygunClient to chain other methods.</returns>
    public static RaygunClient Initialize(string apiKey)
    {
      if (_client == null)
      {
        _client = new RaygunClient(apiKey);
      }

      return _client;
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// Native iOS exception reporting is not enabled with this method, an overload is available to do so.
    /// </summary>
    /// <returns>The RaygunClient to chain other methods.</returns>
    public RaygunClient AttachCrashReporting()
    {
      return AttachCrashReporting(false);
    }

    /// <summary>
    /// Causes Raygun to listen to and send all unhandled exceptions and unobserved task exceptions.
    /// </summary>
    /// <param name="canReportNativeErrors">Whether or not to listen to and report native exceptions.</param>
    /// <returns>The RaygunClient to chain other methods.</returns>
    public RaygunClient AttachCrashReporting(bool reportNativeErrors)
    {
      Debug.WriteLine("Raygun: Initialising crash reporting");
      RaygunClient.DetachCrashReporting();

      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

      if (reportNativeErrors)
      {
        NativeClient = NativeRaygunClient.SharedInstanceWithApiKey(_apiKey);
      }

      return this;
    }

    /// <summary>
    /// Causes Raygun to automatically send session and view events for Raygun Pulse.
    /// </summary>
    /// <returns>The RaygunClient to chain other methods.</returns>
    public RaygunClient AttachPulse()
    {
      Pulse.Attach(this);
      return this;
    }

    /// <summary>
    /// Detaches Raygun from listening to unhandled exceptions and unobserved task exceptions.
    /// </summary>
    public static void Detach()
    {
      AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Detaches Raygun from listening to unhandled exceptions and unobserved task exceptions.
    /// </summary>
    public static void DetachCrashReporting()
    {
      AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Detaches Raygun from automatically sending session and view events to Raygun Pulse.
    /// </summary>
    public static void DetachPulse()
    {
      Pulse.Detach();
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
        _client.Send(e.ExceptionObject as Exception, new List<string>() { "UnhandledException" });

        Pulse.SendRemainingViews();
      }
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var message = RaygunMessageBuilder.New
        .SetEnvironmentDetails()
        .SetMachineName(MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(GetVersion())
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo)
        .Build();

      var customGroupingKey = OnCustomGroupingKey(exception, message);
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData, int timeout)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData), timeout);
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
    public override void Send(RaygunMessage raygunMessage)
    {
      Send(raygunMessage, SynchronousTimeout);
    }

    private void Send(RaygunMessage raygunMessage, int timeout)
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
            Debug.WriteLine(string.Format("Error serializing message {0}", ex.Message));
          }

          if (message != null)
          {
            try
            {
              SaveMessage(message);
            }
            catch (Exception ex)
            {
              Debug.WriteLine(string.Format("Error saving Exception to device {0}", ex.Message));
              if (HasInternetConnection)
              {
                SendMessage(message, timeout);
              }
            }

            // In the case of sending messages during a crash, only send stored messages if there are 2 or less.
            // This is to prevent keeping the app open for a long time while it crashes.
            if (HasInternetConnection && GetStoredMessageCount() <= 2)
            {
              SendStoredMessages(timeout);
            }
          }
        }
      }
    }

    public void Crash()
    {
      NativeClient.Crash();
    }

    #region Real User Monitoring

    private string GenerateNewSessionId()
    {
      return Guid.NewGuid().ToString();
    }

    public void EnsurePulseSessionStarted()
    {
      if (string.IsNullOrEmpty(_sessionId))
      {
        SendPulseSessionEventNow(RaygunPulseSessionEventType.SessionStart);
      }
    }

    public void EnsurePulseSessionEnded()
    {
      if (!string.IsNullOrEmpty(_sessionId))
      {
        SendPulseSessionEventNow(RaygunPulseSessionEventType.SessionEnd);
      }
    }

    private RaygunPulseMessage BuildPulseMessage(RaygunPulseSessionEventType type)
    {
      var msg = new RaygunPulseMessage();
      var data = new RaygunPulseDataMessage();

      data.Timestamp = DateTime.UtcNow;
      data.Version = GetVersion();
      data.OS = UIDevice.CurrentDevice.SystemName;
      data.OSVersion = UIDevice.CurrentDevice.SystemVersion;
      data.Platform = Mindscape.Raygun4Net.Builders.RaygunEnvironmentMessageBuilder.GetStringSysCtl("hw.machine");
      data.User = UserInfo;

      msg.EventData = new[] { data };

      switch (type)
      {
        case RaygunPulseSessionEventType.SessionStart:
          data.Type = "session_start";
          break;
        case RaygunPulseSessionEventType.SessionEnd:
          data.Type = "session_end";
          break;
      }

      data.SessionId = _sessionId;

      return msg;
    }

    internal void SendPulseSessionEventNow(RaygunPulseSessionEventType type)
    {
      if (type == RaygunPulseSessionEventType.SessionStart)
      {
        _sessionId = GenerateNewSessionId();
      }

      var message = BuildPulseMessage(type);
      Send(message);

      if (type == RaygunPulseSessionEventType.SessionEnd)
      {
        _sessionId = null;
      }
    }

    /// <summary>
    /// Sends a Pulse session event to Raygun. The message is sent on a background thread.
    /// </summary>
    /// <param name="eventType">The type of session event that occurred.</param>
    internal void SendPulseSessionEvent(RaygunPulseSessionEventType type)
    {
      if (type == RaygunPulseSessionEventType.SessionStart)
      {
        _sessionId = GenerateNewSessionId();
      }

      var message = BuildPulseMessage(type);
      ThreadPool.QueueUserWorkItem(c => Send(message));

      if (type == RaygunPulseSessionEventType.SessionEnd)
      {
        _sessionId = null;
      }
    }

    internal void SendPulseTimingEventNow(RaygunPulseEventType eventType, string name, long milliseconds)
    {
      SendPulseTimingEventCore(eventType, name, milliseconds);
    }

    /// <summary>
    /// Sends a pulse timing event to Raygun. The message is sent on a background thread.
    /// </summary>
    /// <param name="eventType">The type of event that occurred.</param>
    /// <param name="name">The name of the event resource such as the view name or URL of a network call.</param>
    /// <param name="milliseconds">The duration of the event in milliseconds.</param>
    public void SendPulseTimingEvent(RaygunPulseEventType eventType, string name, long milliseconds)
    {
      lock (_batchLock)
      {
        try
        {
          if (_activeBatch == null)
          {
            _activeBatch = new PulseEventBatch(this);
          }

          if (_activeBatch != null && !_activeBatch.IsLocked)
          {
            EnsurePulseSessionStarted();

            PendingEvent pendingEvent = new PendingEvent(eventType, name, milliseconds, _sessionId);
            _activeBatch.Add(pendingEvent);
          }
          else
          {
            ThreadPool.QueueUserWorkItem(c => SendPulseTimingEventCore(eventType, name, milliseconds));
          }
        }
        catch (Exception e)
        {
          Debug.WriteLine(string.Format("Error sending pulse timing event to Raygun: {0}", e.Message));
        }
      }
    }

    internal void Send(PulseEventBatch batch)
    {
      ThreadPool.QueueUserWorkItem(c => SendCore(batch));
      _activeBatch = null;
    }

    private void SendCore(PulseEventBatch batch)
    {
      try
      {
        EnsurePulseSessionStarted();

        string version = GetVersion();
        string os = UIDevice.CurrentDevice.SystemName;
        string osVersion = UIDevice.CurrentDevice.SystemVersion;
        string platform = Mindscape.Raygun4Net.Builders.RaygunEnvironmentMessageBuilder.GetStringSysCtl("hw.machine");

        RaygunPulseMessage message = new RaygunPulseMessage();

        Debug.WriteLine("BatchSize: " + batch.PendingEventCount);

        RaygunPulseDataMessage[] eventMessages = new RaygunPulseDataMessage[batch.PendingEventCount];
        int index = 0;

        foreach (PendingEvent pendingEvent in batch.PendingEvents)
        {
          RaygunPulseDataMessage dataMessage = new RaygunPulseDataMessage();
          dataMessage.SessionId = pendingEvent.SessionId;
          dataMessage.Timestamp = pendingEvent.Timestamp;
          dataMessage.Version = version;
          dataMessage.OS = os;
          dataMessage.OSVersion = osVersion;
          dataMessage.Platform = platform;
          dataMessage.Type = "mobile_event_timing";
          dataMessage.User = batch.UserInfo;

          string type = pendingEvent.EventType == RaygunPulseEventType.ViewLoaded ? "p" : "n";

          RaygunPulseData data = new RaygunPulseData()
          {
            Name = pendingEvent.Name,
            Timing = new RaygunPulseTimingMessage() { Type = type, Duration = pendingEvent.Duration }
          };

          RaygunPulseData[] dataArray = { data };
          string dataStr = SimpleJson.SerializeObject(dataArray);
          dataMessage.Data = dataStr;

          eventMessages[index] = dataMessage;
          index++;
        }
        message.EventData = eventMessages;

        Send(message);
      }
      catch (Exception e)
      {
        Debug.WriteLine(string.Format("Error sending pulse event batch to Raygun: {0}", e.Message));
      }
    }

    private void SendPulseTimingEventCore(RaygunPulseEventType eventType, string name, long milliseconds)
    {
      EnsurePulseSessionStarted();

      RaygunPulseMessage message = new RaygunPulseMessage();
      RaygunPulseDataMessage dataMessage = new RaygunPulseDataMessage();

      dataMessage.SessionId = _sessionId;
      dataMessage.Timestamp = DateTime.UtcNow - TimeSpan.FromMilliseconds(milliseconds);
      dataMessage.Version = GetVersion();
      dataMessage.OS = UIDevice.CurrentDevice.SystemName;
      dataMessage.OSVersion = UIDevice.CurrentDevice.SystemVersion;
      dataMessage.Platform = Mindscape.Raygun4Net.Builders.RaygunEnvironmentMessageBuilder.GetStringSysCtl("hw.machine");
      dataMessage.Type = "mobile_event_timing";
      dataMessage.User = UserInfo;

      string type = eventType == RaygunPulseEventType.ViewLoaded ? "p" : "n";

      RaygunPulseData data = new RaygunPulseData()
      {
        Name = name,
        Timing = new RaygunPulseTimingMessage() { Type = type, Duration = milliseconds }
      };

      RaygunPulseData[] dataArray = { data };
      string dataStr = SimpleJson.SerializeObject(dataArray);
      dataMessage.Data = dataStr;

      message.EventData = new[] { dataMessage };

      Send(message);
    }

    private void Send(RaygunPulseMessage raygunPulseMessage)
    {
      if (ValidateApiKey())
      {
        string message = null;
        try
        {
          message = SimpleJson.SerializeObject(raygunPulseMessage);
        }
        catch (Exception ex)
        {
          Debug.WriteLine(string.Format("Error serializing message {0}", ex.Message));
        }

        if (message != null)
        {
          SendPulseMessage(message);
        }
      }
    }

    private bool SendPulseMessage(string message)
    {
      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);
        client.Headers.Add("content-type", "application/json; charset=utf-8");
        client.Encoding = System.Text.Encoding.UTF8;

        try
        {
          client.UploadStringAsync(RaygunSettings.Settings.PulseEndpoint, message);
        }
        catch (Exception ex)
        {
          Debug.WriteLine(string.Format("Error Logging Pulse message to Raygun.io {0}", ex.Message));
          return false;
        }
      }
      return true;
    }

    #endregion

    private bool SendMessage(string message, int timeout)
    {
      using (var client = new TimeoutWebClient(timeout))
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
          Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));
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

    private void SendStoredMessages(int timeout)
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
                  bool success = SendMessage(text, timeout);
                  // If just one message fails to send, then don't delete the message, and don't attempt sending anymore until later.
                  if (!success)
                  {
                    return;
                  }
                  Debug.WriteLine("Sent " + name);
                }
                isolatedStorage.DeleteFile(name);
              }
              if (isolatedStorage.GetFileNames("RaygunIO\\*.txt").Length == 0)
              {
                Debug.WriteLine("Successfully sent all pending messages");
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

    private int GetStoredMessageCount()
    {
      try
      {
        using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
        {
          if (isolatedStorage.DirectoryExists("RaygunIO"))
          {
            string[] fileNames = isolatedStorage.GetFileNames("RaygunIO\\*.txt");
            return fileNames.Length;
          }
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine(string.Format("Error getting stored message count: {0}", ex.Message));
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
          Debug.WriteLine("Saved message: " + "RaygunErrorMessage" + number + ".txt");
          Debug.WriteLine("File Count: " + isolatedStorage.GetFileNames("RaygunIO\\*.txt").Length);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }

    private class TimeoutWebClient : WebClient
    {
      private readonly int _timeout;

      public TimeoutWebClient(int timeout)
      {
        _timeout = timeout;
      }

      protected override WebRequest GetWebRequest(Uri address)
      {
        WebRequest request = base.GetWebRequest(address);
        if (_timeout > 0)
        {
          request.Timeout = _timeout;
        }
        return request;
      }
    }
  }
}
