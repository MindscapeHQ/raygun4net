﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{

  public abstract class RaygunClientBase
  {
    protected internal const string SentKey = "AlreadySentByRaygun";

    private static HttpClient Client = new HttpClient();
    private bool _handlingRecursiveErrorSending;
    private bool _handlingRecursiveGrouping;

    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();
    protected readonly RaygunSettingsBase Settings;


    /// <summary>
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;

    /// <summary>
    /// Raised before a message is sent. This can be used to add a custom grouping key to a RaygunMessage before sending it to the Raygun service.
    /// </summary>
    public event EventHandler<RaygunCustomGroupingKeyEventArgs> CustomGroupingKey;

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public virtual string User { get; set; }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public virtual RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

#if NETSTANDARD2_0_OR_GREATER
    /// <summary>
    /// If set to true, this will automatically setup handlers to catch Unhandled Exceptions
    /// </summary>
    /// <remarks>
    /// Currently defaults to false. This may be change in future releases.
    /// </remarks>
    public virtual bool CatchUnhandledExceptions
    {
      get { return Settings.CatchUnhandledExceptions; }

      set
      {
        if (Settings.CatchUnhandledExceptions == value) { return; }

        if (value)
        {
          System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        else
        {
          System.AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }

        Settings.CatchUnhandledExceptions = value;


        void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
          Send(e.ExceptionObject as Exception, new[] { "UnhandledException" });
        }
      }
    }

#endif

    protected RaygunClientBase(RaygunSettingsBase settings, HttpClient client) : this(settings)
    {
      Client = client;
    }

    public RaygunClientBase(RaygunSettingsBase settings)
    {
      Settings = settings;
      _apiKey = settings.ApiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));

      if (!string.IsNullOrEmpty(settings.ApplicationVersion))
      {
        ApplicationVersion = settings.ApplicationVersion;
      }

#if NETSTANDARD2_0_OR_GREATER
      if (Settings.CatchUnhandledExceptions)
      {
         CatchUnhandledExceptions = Settings.CatchUnhandledExceptions;
      }
#endif
    }


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

    protected virtual bool CanSend(Exception exception)
    {
      return exception == null || exception.Data == null || !exception.Data.Contains(SentKey) ||
             false.Equals(exception.Data[SentKey]);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception != null && exception.Data != null)
      {
        try
        {
          Type[] genericTypes = exception.Data.GetType().GetTypeInfo().GenericTypeArguments;

          if (genericTypes.Length == 0 || genericTypes[0].GetTypeInfo().IsAssignableFrom(typeof(string)))
          {
            exception.Data[SentKey] = true;
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to flag exception as sent: {ex.Message}");
        }
      }
    }

    // Returns true if the message can be sent, false if the sending is canceled.
    protected bool OnSendingMessage(RaygunMessage raygunMessage)
    {
      bool result = true;

      if (!_handlingRecursiveErrorSending)
      {
        var handler = SendingMessage;

        if (handler != null)
        {
          var args = new RaygunSendingMessageEventArgs(raygunMessage);

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

    protected async Task<string> OnCustomGroupingKey(Exception exception, RaygunMessage message)
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

            await SendAsync(e, null, null).ConfigureAwait(false);

            _handlingRecursiveGrouping = false;
          }

          result = args.CustomGroupingKey;
        }
      }

      return result;
    }

    protected bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }

      return true;
    }

    protected virtual bool CanSend(RaygunMessage message)
    {
      return true;
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void Send(Exception exception)
    {
      Send(exception, null, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, null);
    }

    /// <summary>
    /// Transmits an exception to Raygun synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      try
      {
        SendAsync(exception, tags, userCustomData).GetAwaiter().GetResult();
      }
      catch
      {
        // Swallow the exception if we're not throwing on error
        if (Settings.ThrowOnError)
        {
          throw;
        }
      }
    }

    /// <summary>
    /// Transmits an exception to Raygun asynchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="userInfo">Information about the user including the identity string.</param>
    public virtual async Task SendAsync(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo = null)
    {
      if (CanSend(exception))
      {
        try
        {
          await StripAndSend(exception, tags, userCustomData, userInfo).ConfigureAwait(false);
          FlagAsSent(exception);
        }
        catch
        {
          // Swallow the exception if we're not throwing on error
          if (Settings.ThrowOnError)
          {
            throw;
          }
        }
      }
    }

    /// <summary>
    /// Transmits an exception to Raygun in the background without blocking.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="userInfo">Information about the user including the identity string.</param>
    public virtual Task SendInBackground(Exception exception, IList<string> tags = null,
      IDictionary userCustomData = null, RaygunIdentifierMessage userInfo = null)
    {
      if (CanSend(exception))
      {
        // For backwards compatibility we need to continue to support the old SendInBackground method signature.
        // So we just fire and forget and discard the task.
        // If we await this Task it will cause SendInBackground to be blocking.
        _ = Task.Run(async () =>
        {
          await StripAndSend(exception, tags, userCustomData, userInfo);
        });

        FlagAsSent(exception);
      }

      return Task.CompletedTask;
    }

    /// <summary>
    /// Transmits an exception to Raygun in the background without blocking.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public Task SendInBackground(RaygunMessage raygunMessage)
    {
      // For backwards compatibility we need to continue to support the old SendInBackground method signature.
      // So we just fire and forget and discard the task.
      // If we await this Task it will cause SendInBackground to be blocking.
      _ = Task.Run(() => Send(raygunMessage));
      return Task.CompletedTask;
    }

    internal void FlagExceptionAsSent(Exception exception)
    {
      FlagAsSent(exception);
    }

    protected virtual async Task<RaygunMessage> BuildMessage(Exception exception, IList<string> tags,
      IDictionary userCustomData, RaygunIdentifierMessage userInfo)
    {
      var message = RaygunMessageBuilder.New(Settings)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(userInfo ?? UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();

      var customGroupingKey = await OnCustomGroupingKey(exception, message).ConfigureAwait(false);

      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }

    protected async Task StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData,
      RaygunIdentifierMessage userInfo)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        await Send(await BuildMessage(e, tags, userCustomData, userInfo).ConfigureAwait(false)).ConfigureAwait(false);
      }
    }

    protected IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException =>
            exception.GetType() == wrapperException && exception.InnerException != null))
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
    /// Posts a RaygunMessage to the Raygun api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public async Task Send(RaygunMessage raygunMessage)
    {

      if (!ValidateApiKey())
      {
        return;
      }

      bool canSend = OnSendingMessage(raygunMessage) && CanSend(raygunMessage);

      if (!canSend)
      {
        return;
      }

      var requestMessage = new HttpRequestMessage(HttpMethod.Post, Settings.ApiEndpoint);
      requestMessage.Headers.Add("X-ApiKey", _apiKey);

      try
      {
        var message = SimpleJson.SerializeObject(raygunMessage);
        requestMessage.Content = new StringContent(message, Encoding.UTF8, "application/json");

        var result = await Client.SendAsync(requestMessage).ConfigureAwait(false);

        if (!result.IsSuccessStatusCode)
        {
          Debug.WriteLine($"Error Logging Exception to Raygun {result.ReasonPhrase}");

          if (Settings.ThrowOnError)
          {
            throw new Exception("Could not log to Raygun");
          }
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error Logging Exception to Raygun {ex.Message}");

        if (Settings.ThrowOnError)
        {
          throw;
        }
      }
    }


  }
}