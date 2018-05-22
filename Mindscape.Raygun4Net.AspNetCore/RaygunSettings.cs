using System;

namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunSettings : Raygun4Net.RaygunSettings
  {
    public RaygunSettings()
    {
      ApiEndpoint = new Uri(DefaultApiEndPoint);
    }

    public bool MediumTrust { get; set; }

    public int[] ExcludedStatusCodes { get; set; }

    public bool ExcludeErrorsFromLocal { get; set; }

    public string[] IgnoreFormFieldNames { get; set; }

    public string[] IgnoreHeaderNames { get; set; }

    public string[] IgnoreCookieNames { get; set; }

    public string[] IgnoreServerVariableNames { get; set; }

    public bool IsRawDataIgnored { get; set; }
    
    public bool ReplaceUnseekableRequestStreams { get; set; }
  }
}
