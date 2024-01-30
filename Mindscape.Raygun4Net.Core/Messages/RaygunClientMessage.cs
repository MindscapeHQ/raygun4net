using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      object[] attributes = GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
      Name = attributes.Length > 0 ? ((AssemblyTitleAttribute)attributes[0]).Title : "Raygun4Net";
      Version = new AssemblyName(GetType().Assembly.FullName).Version.ToString();
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunClientMessage: Name={0}, Version={1}, ClientUrl={2}]", Name, Version, ClientUrl);
    }
  }
}