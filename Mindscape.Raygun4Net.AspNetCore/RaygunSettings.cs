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

    public bool MediumTrust { get; set; }

    public bool ThrowOnError { get; set; }

    public int[] ExcludedStatusCodes { get; set; }

    public bool ExcludeErrorsFromLocal { get; set; }

    public string[] IgnoreFormFieldNames { get; set; }

    public string[] IgnoreHeaderNames { get; set; }

    public string[] IgnoreCookieNames { get; set; }

    public string[] IgnoreServerVariableNames { get; set; }

    public bool IsRawDataIgnored { get; set; }

    public string ApplicationVersion { get; set; }
    
    public bool ReplaceUnSeekableRequestStreams { get; set; }
  }
}
