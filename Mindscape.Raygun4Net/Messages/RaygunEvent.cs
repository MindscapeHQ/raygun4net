using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEvent
  {
    public RaygunEvent()
    {
      Parameters = new Dictionary<string, string>();
    }

    public string Name { get; set; }
    public string ContextId { get; set; }
    public string Source { get; set; }
    public DateTime EventTime { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
  }
}
