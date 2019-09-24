﻿using System.ComponentModel;

namespace Mindscape.Raygun4Net
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
