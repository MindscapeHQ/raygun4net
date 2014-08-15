using System.Reflection;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public static class RaygunClientMessageBuilder
  {
    private static RaygunClientMessage _raygunClientMessage = new RaygunClientMessage()
    {
        Name = "Raygun4Net",
        Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString(),
        ClientUrl = @"https://github.com/MindscapeHQ/raygun4net"
    };

    public static RaygunClientMessage Build()
    {
      return _raygunClientMessage;
    }
  }
}
