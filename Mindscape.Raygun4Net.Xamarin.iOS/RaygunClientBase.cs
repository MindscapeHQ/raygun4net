using System;
using System.Net;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public abstract class RaygunClientBase
  {
    protected internal const string SentKey = "AlreadySentByRaygun";

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public virtual string User { get; set; }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public virtual RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets the context identifier defining a scope under which errors are related
    /// </summary>
    public string ContextId { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

    protected virtual bool CanSend(Exception exception)
    {
      return exception == null || exception.Data == null || !exception.Data.Contains(SentKey) || false.Equals(exception.Data[SentKey]);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception != null && exception.Data != null)
      {
        try
        {
          Type[] genericTypes = exception.Data.GetType().GetGenericArguments();
          if (genericTypes.Length == 0 || genericTypes[0].IsAssignableFrom(typeof(string)))
          {
            exception.Data[SentKey] = true;
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(String.Format("Failed to flag exception as sent: {0}", ex.Message));
        }
      }
    }

    /// <summary>
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;

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
      if(!_handlingRecursiveGrouping)
      {
        var handler = CustomGroupingKey;
        if(handler != null)
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
    /// Transmits an exception to Raygun.io synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public abstract void Send(Exception exception);

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    /// <param name="exception">The original exception used to generate the RaygunMessage</param>
    public abstract void Send(RaygunMessage raygunMessage);
  }
}
