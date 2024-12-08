﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Breadcrumbs;

namespace Mindscape.Raygun4Net
{
  public abstract class RaygunClientBase
  {
    /// <summary>
    /// If no HttpClient is provided to the constructor, this will be used.
    /// </summary>
    private static readonly HttpClient DefaultClient = new()
    {
      // The default timeout is 100 seconds for the HttpClient, 
      Timeout = TimeSpan.FromSeconds(30)
    };
    
    private IEnumerable<IMessageBuilder> _messageBuilders = [];

    /// <summary>
    /// This is the HttpClient that will be used to send messages to the Raygun endpoint.
    /// </summary>
    private readonly HttpClient _client;

    private readonly List<Type> _wrapperExceptions = new();

    private bool _handlingRecursiveErrorSending;
    private bool _handlingRecursiveGrouping;

    protected readonly RaygunSettingsBase _settings;
    private readonly ThrottledBackgroundMessageProcessor _backgroundMessageProcessor;
    private readonly IRaygunUserProvider _userProvider;
    protected internal const string SentKey = "AlreadySentByRaygun";

    /// <summary>
    /// Store a strong reference to the OnApplicationUnhandledException delegate, so it does not get garbage collected while
    /// the client is still alive
    /// </summary>
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly UnhandledExceptionBridge.UnhandledExceptionHandlerWithTag _onUnhandledExceptionDelegate;


    /// <summary>
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/> or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;

    /// <summary>
    /// Raised before a message is sent. This can be used to add a custom grouping key to a RaygunMessage before sending it to the Raygun service.
    /// </summary>
    public event EventHandler<RaygunCustomGroupingKeyEventArgs> CustomGroupingKey;

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    [Obsolete("Provide a `IRaygunUserProvider` to the RaygunClient constructor instead.")]
    public virtual string User { get; set; }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    [Obsolete("Provide a `IRaygunUserProvider` to the RaygunClient constructor instead.")]
    public virtual RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun endpoint.
    /// </summary>
    [Obsolete("Use the `RaygunSettings.ApplicationVersion` property instead.")]
    public string ApplicationVersion
    {
      get => _settings.ApplicationVersion;
      set => _settings.ApplicationVersion = value;
    }

    /// <summary>
    /// If set to true, this will automatically setup handlers to send Unhandled Exceptions to Raygun  
    /// </summary>
    /// <remarks>
    /// Currently defaults to false. This may be change in future releases.
    /// </remarks>
    [Obsolete("Use the `RaygunSettings.CatchUnhandledExceptions` property instead.")]
    public virtual bool CatchUnhandledExceptions
    {
      get => _settings.CatchUnhandledExceptions;
      set => _settings.CatchUnhandledExceptions = value;
    }

    private void OnApplicationUnhandledException(Exception exception, bool isTerminating, string tag)
    {
      if (!_settings.CatchUnhandledExceptions)
      {
        return;
      }

      Send(exception, new List<string> { tag });
    }

    protected RaygunClientBase(RaygunSettingsBase settings)
      : this(settings, DefaultClient, null, [])
    {
    }

    // ReSharper disable once IntroduceOptionalParameters.Global
    protected RaygunClientBase(RaygunSettingsBase settings, HttpClient client)
      : this(settings, client, null, [])
    {
    }

    protected RaygunClientBase(RaygunSettingsBase settings, IRaygunUserProvider userProvider, IEnumerable<IMessageBuilder> messageBuilders)
      : this(settings, DefaultClient, userProvider, messageBuilders)
    {
    }

    protected RaygunClientBase(RaygunSettingsBase settings, HttpClient client, IRaygunUserProvider userProvider, IEnumerable<IMessageBuilder> messageBuilders)
    {
      _messageBuilders = messageBuilders ?? [];
      
      _client = client ?? DefaultClient;
      _settings = settings;
      _backgroundMessageProcessor = new ThrottledBackgroundMessageProcessor(settings.BackgroundMessageQueueMax, _settings.BackgroundMessageWorkerCount, _settings.BackgroundMessageWorkerBreakpoint, Send);
      _userProvider = userProvider;

      _wrapperExceptions.Add(typeof(TargetInvocationException));

      _onUnhandledExceptionDelegate = OnApplicationUnhandledException;

      UnhandledExceptionBridge.OnUnhandledException(_onUnhandledExceptionDelegate);

      if (settings.OfflineStore != null)
      {
        settings.OfflineStore.SetSendCallback(SendOfflinePayloadAsync);
      }
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
      foreach (var wrapper in wrapperExceptions)
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
      foreach (var wrapper in wrapperExceptions)
      {
        _wrapperExceptions.Remove(wrapper);
      }
    }

    protected virtual bool CanSend(Exception exception)
    {
      return exception?.Data == null || !exception.Data.Contains(SentKey) ||
             false.Equals(exception.Data[SentKey]);
    }

    protected bool Enqueue(RaygunMessage msg)
    {
      return _backgroundMessageProcessor.Enqueue(msg);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception?.Data != null)
      {
        try
        {
          var genericTypes = exception.Data.GetType().GetTypeInfo().GenericTypeArguments;

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
      var result = true;

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
      if (string.IsNullOrEmpty(_settings.ApiKey))
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
    [Obsolete("Please use SendAsync() to avoid possible deadlocks")]
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
    [Obsolete("Please use SendAsync() to avoid possible deadlocks")]
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
    [Obsolete("Please use SendAsync() to avoid possible deadlocks")]
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      try
      {
        SendAsync(exception, tags, userCustomData).ConfigureAwait(false).GetAwaiter().GetResult();
      }
      catch
      {
        // Swallow the exception if we're not throwing on error
        if (_settings.ThrowOnError)
        {
          throw;
        }
      }
    }

    /// <summary>
    /// Transmits an exception to Raygun asynchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public async Task SendAsync(Exception exception)
    {
      await SendAsync(exception, null, null).ConfigureAwait(false);
    }

    /// <summary>
    /// Transmits an exception to Raygun asynchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public async Task SendAsync(Exception exception, IList<string> tags)
    {
      await SendAsync(exception, tags, null).ConfigureAwait(false);
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
          if (_settings.ThrowOnError)
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
    public virtual async Task SendInBackground(Exception exception, IList<string> tags = null, IDictionary userCustomData = null, RaygunIdentifierMessage userInfo = null)
    {
      if (CanSend(exception))
      {
        var exceptions = StripWrapperExceptions(exception);
        foreach (var ex in exceptions)
        {
          // Have to do this up front as the IUserProvider may be dependent on contextual information from HttpContext
          // which can get lost if the execution of the BuildMessage is done in a delegate.
          var msg = await BuildMessage(ex, tags, userCustomData, userInfo);
          if (!_backgroundMessageProcessor.Enqueue(msg))
          {
            Debug.WriteLine("Could not add message to background queue. Dropping exception: {0}", ex);
          }
        }

        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Transmits an exception to Raygun in the background without blocking.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public Task SendInBackground(RaygunMessage raygunMessage)
    {
      if (!_backgroundMessageProcessor.Enqueue(raygunMessage))
      {
        Debug.WriteLine("Could not add message to background queue. Dropping message: {0}", raygunMessage);
      }

      return Task.CompletedTask;
    }

    protected async Task<RaygunMessage> BuildMessage(Exception exception,
                                                     IList<string> tags,
                                                     IDictionary userCustomData = null,
                                                     RaygunIdentifierMessage userInfo = null,
                                                     Action<RaygunMessage> customiseMessage = null)
    {
      var message = RaygunMessageBuilder.New(_settings)
                                        .SetEnvironmentDetails()
                                        .SetMachineName(Environment.MachineName)
                                        .SetExceptionDetails(exception)
                                        .SetClientDetails()
                                        .SetVersion(_settings.ApplicationVersion)
                                        .SetTags(tags)
                                        .SetUserCustomData(userCustomData)
                                        .SetUser(userInfo ?? _userProvider?.GetUser() ?? UserInfo ?? (!string.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
                                        .SetBreadcrumbs(RaygunBreadcrumbs.ToList())
                                        .Customise(customiseMessage)
                                        .Build();

      var customGroupingKey = await OnCustomGroupingKey(exception, message).ConfigureAwait(false);

      // TODO - Move old message builders to IMessageBuilder
      foreach (var builder in _messageBuilders)
      {
        await builder.Apply(message, exception);
      }

      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }

    protected virtual async Task StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo)
    {
      foreach (var e in StripWrapperExceptions(exception))
      {
        await Send(await BuildMessage(e, tags, userCustomData, userInfo).ConfigureAwait(false)).ConfigureAwait(false);
      }
    }

    protected virtual IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        var aggregate = exception as AggregateException;

        if (aggregate != null)
        {
          foreach (var e in aggregate.InnerExceptions)
          {
            foreach (var ex in StripWrapperExceptions(e))
            {
              yield return ex;
            }
          }
        }
        else
        {
          foreach (var e in StripWrapperExceptions(exception.InnerException))
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
    public Task Send(RaygunMessage raygunMessage)
    {
      return Send(raygunMessage, CancellationToken.None);
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    /// <param name="cancellationToken"></param>
    public async Task Send(RaygunMessage raygunMessage, CancellationToken cancellationToken)
    {
      if (!ValidateApiKey())
      {
        return;
      }

      var canSend = OnSendingMessage(raygunMessage) && CanSend(raygunMessage);

      if (!canSend)
      {
        return;
      }

      try
      {
        var messagePayload = SimpleJson.SerializeObject(raygunMessage);
        await SendPayloadAsync(messagePayload, _settings.ApiKey, true, cancellationToken).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        if (_settings.ThrowOnError)
        {
          throw;
        }
      }
    }

    private async Task SendOfflinePayloadAsync(string payload, string apiKey, CancellationToken cancellationToken)
    {
      await SendPayloadAsync(payload, apiKey, false, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendPayloadAsync(string payload, string apiKey, bool useOfflineStore, CancellationToken cancellationToken)
    {
      HttpResponseMessage response = null;
      var requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint);
      requestMessage.Headers.Add("X-ApiKey", apiKey);
      requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");

      try
      {
        response = await _client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        // Raises an HttpRequestException if the request was unsuccessful
        response.EnsureSuccessStatusCode();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error Logging Exception to Raygun: {ex.Message}");

        // If we got no response or an unexpected server error then add it to offline storage to send later
        // we get no response if the send call fails for any other reason (network etc.)
        // checking that response.StatusCode >= 500, is an efficient check for any server errors
        var shouldStoreMessage = response is null || response.StatusCode >= HttpStatusCode.InternalServerError;

        if (useOfflineStore && shouldStoreMessage)
        {
          await SaveMessageToOfflineCache(payload, apiKey, cancellationToken);
        }

        throw;
      }
    }

    private async Task<bool> SaveMessageToOfflineCache(string messagePayload, string apiKey, CancellationToken cancellationToken)
    {
      // Can't store it anywhere
      if (_settings.OfflineStore is null)
      {
        return false;
      }

      return await _settings.OfflineStore.Save(messagePayload, apiKey, cancellationToken).ConfigureAwait(false);
    }
  }
}