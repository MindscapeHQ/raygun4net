using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = ((AssemblyTitleAttribute)GetType().GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title;
      Version = new AssemblyName(GetType().GetTypeInfo().Assembly.FullName).Version.ToString();
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}