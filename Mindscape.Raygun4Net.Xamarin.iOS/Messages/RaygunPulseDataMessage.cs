using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunPulseDataMessage
  {
    public string SessionId{ get; set; }

    public DateTime Timestamp{ get; set; }

    public string Type{ get; set; }

    public string Version{ get; set; }
  }
}

