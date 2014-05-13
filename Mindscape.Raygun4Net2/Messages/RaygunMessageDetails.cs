using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public string Version { get; set; }

    public RaygunErrorMessage Error { get; set; }

    public RaygunEnvironmentMessage Environment { get; set; }

    public RaygunClientMessage Client { get; set; }

    public IList<string> Tags { get; set; }

    public IDictionary UserCustomData { get; set; }

    public RaygunIdentifierMessage User { get; set; }
  }
}
