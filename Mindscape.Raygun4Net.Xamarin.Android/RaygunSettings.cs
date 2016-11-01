using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private static RaygunSettings settings;
    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";
    private const string DefaultPulseEndPoint = "https://api.raygun.io/events";

    public static RaygunSettings Settings
    {
      get
      {
        return settings ?? (settings = new RaygunSettings { ApiEndpoint = new Uri(DefaultApiEndPoint), PulseEndpoint = new Uri(DefaultPulseEndPoint) });
      }
    }

    public Uri ApiEndpoint { get; set; }
    public Uri PulseEndpoint { get; set; }
  }
}
