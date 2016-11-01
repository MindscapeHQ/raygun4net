using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunPulseDataMessage
  {
    public string SessionId{ get; set; }

    public DateTime Timestamp{ get; set; }

    public string Type{ get; set; }

    public RaygunIdentifierMessage User { get; set; }

    public string Version{ get; set; }

    public string OS { get; set; }

    public string OSVersion { get; set; }

    public string Platform { get; set; }

    public string Data{ get; set; }
  }
}

