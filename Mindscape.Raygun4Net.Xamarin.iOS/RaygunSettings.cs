using System;
using System.IO;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private static RaygunSettings settings;
    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";
    private const string DefaultPulseEndPoint = "https://api.raygun.io/events";
    private const string DefaultRaygunDirectory = "RaygunIO";

    public static RaygunSettings Settings
    {
      get
      {
        return settings ?? (settings = new RaygunSettings {
          ApiEndpoint = new Uri(DefaultApiEndPoint), 
          PulseEndpoint = new Uri(DefaultPulseEndPoint), 
          LogLevel = RaygunLogLevel.Warning,
          RaygunDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), DefaultRaygunDirectory)
        });
      }
    }

    public Uri ApiEndpoint { get; set; }

    public Uri PulseEndpoint{ get; set; }

    public bool SetUnobservedTaskExceptionsAsObserved { get; set; }

    public RaygunLogLevel LogLevel { get; set; }

    public string RaygunDirectory { get; set; }
  }
}
