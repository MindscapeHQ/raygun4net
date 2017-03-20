using System;
namespace Mindscape.Raygun4Net
{
  internal class SessionEvent
  {
    public DateTime Timestamp { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }

    public decimal Duration { get; set; }
  }
}
