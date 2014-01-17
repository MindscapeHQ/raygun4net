using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = "Raygun4Net";
      Version = Assembly.GetExecutingAssembly().FullName;
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}