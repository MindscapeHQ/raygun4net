using System.Reflection;
using System.Linq;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = ((AssemblyTitleAttribute)GetType().GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).First()).Title;
      Version = GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}