using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private const string DefaultApiEndPoint = "https://api.raygun.com/entries";

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; } = new Uri(DefaultApiEndPoint);

    public bool ThrowOnError { get; set; }

    public string ApplicationVersion { get; set; }

    public bool BreadcrumbsLocationRecordingEnabled { get; set; }

    public RaygunBreadcrumbLevel BreadcrumbsLevel { get; set; } = RaygunBreadcrumbLevel.Info;
  }
}
