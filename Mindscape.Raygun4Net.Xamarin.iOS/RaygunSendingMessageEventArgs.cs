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
    
    public RaygunSendingMessageEventArgs(RaygunMessage message)
    {
      _raygunMessage = message;
    }

    public RaygunMessage Message
    {
      get { return _raygunMessage; }
    }
  }
}
