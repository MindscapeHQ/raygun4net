using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = ((AssemblyTitleAttribute)GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;
      Version = new AssemblyName(GetType().Assembly.FullName).Version.ToString();
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}