using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private static readonly RaygunSettings settings = null;

    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

    public static RaygunSettings Settings
    {
      get
      {
        return settings ?? new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint) };
      }
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }
  }
}
