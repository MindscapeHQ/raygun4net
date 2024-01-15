using System.Reflection;

namespace Mindscape.Raygun4Net
{
  public class RaygunClientMessage
  {
    private static readonly string ClientVersion;

    static RaygunClientMessage()
    {
      var informationalVersion = Assembly
                          .GetExecutingAssembly()
                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion;

      ClientVersion = informationalVersion ?? "8.1.0";
    }

    public RaygunClientMessage()
    {
      Name = "Raygun4Net.NetCore";
      Version = ClientVersion;
      ClientUrl = "https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}