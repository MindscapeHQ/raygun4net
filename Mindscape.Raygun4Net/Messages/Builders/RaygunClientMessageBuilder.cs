using System.Reflection;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public static class RaygunClientMessageBuilder
  {
    private static RaygunClientMessage _raygunClientMessage;

    static RaygunClientMessageBuilder()
    {
      _raygunClientMessage.Name = "Raygun4Net";
      _raygunClientMessage.Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString();
      _raygunClientMessage.ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public static RaygunClientMessage Build()
    {
      return _raygunClientMessage;
    }
  }
}
