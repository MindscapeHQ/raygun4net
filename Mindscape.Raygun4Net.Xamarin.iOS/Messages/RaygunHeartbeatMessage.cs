using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunHeartbeatMessage
  {
    public RaygunHeartbeatMessage ()
    {
      Timestamp = DateTime.UtcNow;
    }

    public string SessionId { get; set; }

    public DateTime Timestamp { get; set; }
  }
}

