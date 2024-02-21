using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunSettings : RaygunSettingsBase, IRaygunHttpSettings
{
  public bool MediumTrust { get; set; }

  public int[] ExcludedStatusCodes { get; set; }

  public bool ExcludeErrorsFromLocal { get; set; }

  public List<string> IgnoreSensitiveFieldNames { get; set; } = new();

  public List<string> IgnoreQueryParameterNames { get; set; } = new();

  public List<string> IgnoreFormFieldNames { get; set; } = new();

  public List<string> IgnoreHeaderNames { get; set; } = new();

  public List<string> IgnoreCookieNames { get; set; } = new();

  public List<string> IgnoreServerVariableNames { get; set; } = new();

  public List<IRaygunDataFilter> RawDataFilters { get; } = new();

  public bool IsRawDataIgnored { get; set; }

  public bool IsRawDataIgnoredWhenFilteringFailed { get; set; }

  public bool UseXmlRawDataFilter { get; set; }

  public bool UseKeyValuePairRawDataFilter { get; set; }

  [Obsolete("Raygun Middleware now uses `Request.EnableBuffering()` to allow the request body to be read multiple times. This setting is no longer required.")]
  public bool ReplaceUnseekableRequestStreams { get; set; }
    
  public string ApplicationIdentifier { get; set; }
}