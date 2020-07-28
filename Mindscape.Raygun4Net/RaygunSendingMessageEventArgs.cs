using System;
using System.ComponentModel;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  /// <summary>
  /// Can be used to modify the message before sending, or to cancel the send operation.
  /// </summary>
  public class RaygunSendingMessageEventArgs : CancelEventArgs
  {
    private RaygunMessage _raygunMessage;
    private Exception _exception;

    public RaygunSendingMessageEventArgs(RaygunMessage message)
    : this(message, null)
    {
    }
    
    public RaygunSendingMessageEventArgs(RaygunMessage message, Exception exception)
    {
      _raygunMessage = message;
      _exception = exception;
    }

    public RaygunMessage Message
    {
      get { return _raygunMessage; }
    }
    
    public Exception Exception
    {
      get { return _exception; }
    }
  }
}
