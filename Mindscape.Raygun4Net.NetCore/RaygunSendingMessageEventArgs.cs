using System.ComponentModel;

namespace Mindscape.Raygun4Net
{
  /// <summary>
  /// Can be used to modify the message before sending, or to cancel the send operation.
  /// </summary>
  public class RaygunSendingMessageEventArgs : CancelEventArgs
  {
    public RaygunMessage Message { get; private set; }
    
    public RaygunSendingMessageEventArgs(RaygunMessage message)
    {
      Message = message;
    }
  }
}
