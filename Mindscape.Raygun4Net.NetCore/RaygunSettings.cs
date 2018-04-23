using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private const string DefaultApiEndPoint = "https://api.raygun.com/entries";

    public RaygunSettings()
    {
      ApiEndpoint = new Uri(DefaultApiEndPoint);
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }

    public bool ThrowOnError { get; set; }

    public string ApplicationVersion { get; set; }
  }
}
