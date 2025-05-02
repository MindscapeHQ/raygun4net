using System;
using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    private static string LookupRaygunVersion(Type messageType)
    {
      var lastRaygunVersion = _lastRaygunVersion;
      if (lastRaygunVersion is null || !ReferenceEquals(lastRaygunVersion.Item1, messageType))
        _lastRaygunVersion = lastRaygunVersion = new Tuple<Type, string>(messageType, new AssemblyName(messageType.Assembly.FullName).Version.ToString());
      return lastRaygunVersion.Item2;
    }
    private static Tuple<Type, string> _lastRaygunVersion;

    public RaygunClientMessage()
    {
      Name = "Raygun4Net";
      Version = LookupRaygunVersion(GetType());
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return $"[RaygunClientMessage: Name={Name}, Version={Version}, ClientUrl={ClientUrl}]";
    }
  }
}