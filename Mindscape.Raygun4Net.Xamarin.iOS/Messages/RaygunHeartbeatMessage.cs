using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunHeartbeatMessage
  {
    public RaygunHeartbeatMessage ()
    {
      Type = "session_heartbeat";
      Timestamp = DateTime.UtcNow;
    }

    public string Type { get; set; }

    public string SessionId { get; set; }

    public DateTime Timestamp { get; set; }
  }
}

