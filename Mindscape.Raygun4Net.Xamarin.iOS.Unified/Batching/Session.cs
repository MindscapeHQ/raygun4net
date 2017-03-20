using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  internal class Session
  {
    public DateTime Start { get; set; }

    public DateTime? End { get; set; }

    public string SessionId { get; set; }

    public RaygunIdentifierMessage User { get; set; }

    public string Version { get; set; }

    public string OS { get; set; }

    public string Platform { get; set; }

    public List<SessionEvent> Events { get; set; }
  }
}
