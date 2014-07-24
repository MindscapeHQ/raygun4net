using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEventBatchMessage
  {
    public RaygunEventMessage[] EventData { get; set; }
  }
}
