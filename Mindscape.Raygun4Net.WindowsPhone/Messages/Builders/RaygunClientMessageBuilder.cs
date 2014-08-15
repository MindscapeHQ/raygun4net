using System.Reflection;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunClientMessageBuilder
  {
    public RaygunClientMessage Build()
    {
      var raygunClientMessage = new RaygunClientMessage()
      {
        Name = "Raygun4Net.WindowsPhone",
        Version = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version.ToString(),
        ClientUrl = @"https://github.com/MindscapeHQ/raygun4net"
      };

      return raygunClientMessage;
    }
  }
}
