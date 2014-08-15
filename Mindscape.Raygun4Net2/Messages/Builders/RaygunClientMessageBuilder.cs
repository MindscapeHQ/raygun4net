using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunClientMessageBuilder
  {
    public RaygunClientMessage Build()
    {
      var raygunClientMessage = new RaygunClientMessage()
      {
        Name = "Raygun4Net2.0", // 2 is the version of .Net, not the Raygun provider.
        Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString(),
        ClientUrl = @"https://github.com/MindscapeHQ/raygun4net"
      };

      return raygunClientMessage;
    }
  }
}
