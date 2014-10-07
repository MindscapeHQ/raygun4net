using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Name.Replace("Mindscape.", "");
      Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString();
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}