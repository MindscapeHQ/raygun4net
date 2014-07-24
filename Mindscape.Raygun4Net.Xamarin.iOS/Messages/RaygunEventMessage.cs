using System;

using MonoTouch.UIKit;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEventMessage
  {
    public RaygunEventMessage()
    {
      OS = UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
      Timestamp = DateTime.UtcNow;
      DeviceType = UIDevice.CurrentDevice.SystemName;
    }

    public string Type { get; set; }

    public RaygunIdentifierMessage User { get; set; }

    public string DeviceId { get; set; }

    public string SessionId { get; set; }

    public string Version { get; set; }

    public string OS { get; set; }

    public DateTime Timestamp { get; set;}

    public string DeviceType { get; set; }
  }
}

