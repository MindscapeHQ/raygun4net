﻿namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunSettings : RaygunSettingsBase
  {
    public bool MediumTrust { get; set; }

    public int[] ExcludedStatusCodes { get; set; }

    public bool ExcludeErrorsFromLocal { get; set; }

    public string[] IgnoreSensitiveFieldNames { get; set; }

    public string[] IgnoreQueryParameterNames { get; set; }

    public string[] IgnoreFormFieldNames { get; set; }

    public string[] IgnoreHeaderNames { get; set; }

    public string[] IgnoreCookieNames { get; set; }

    public string[] IgnoreServerVariableNames { get; set; }

    public bool IsRawDataIgnored { get; set; }

    public bool IsRawDataIgnoredWhenFilteringFailed { get; set; }

    public bool UseXmlRawDataFilter { get; set; }

    public bool UseKeyValuePairRawDataFilter { get; set; }

    public bool ReplaceUnseekableRequestStreams { get; set; }
  }
}
