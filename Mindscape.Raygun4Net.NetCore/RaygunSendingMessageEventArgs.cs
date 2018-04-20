using System.ComponentModel;
using Mindscape.Raygun4Net.NetCore.Messages;

namespace Mindscape.Raygun4Net.NetCore
{
  /// <summary>
  /// Can be used to modify the message before sending, or to cancel the send operation.
  /// </summary>
  public class RaygunSendingMessageEventArgs : CancelEventArgs
  {
    public RaygunSendingMessageEventArgs(RaygunMessage message)
    {
      Message = message;
    }

    public RaygunMessage Message { get; private set; }
  }
}
