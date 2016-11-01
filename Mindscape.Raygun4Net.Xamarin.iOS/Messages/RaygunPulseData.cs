using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunPulseData
  {
    public string Name { get; set; }

    public RaygunPulseTimingMessage Timing { get; set; }
  }
}

