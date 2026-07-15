using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunSettings : RaygunSettingsBase, IRaygunHttpSettings
{
  [Obsolete("This is not used by the Raygun4Net.AspNetCore package.")]
  public bool MediumTrust { get; set; }

  public int[] ExcludedStatusCodes { get; set; }

  public bool ExcludeErrorsFromLocal { get; set; }

  /// <summary>
  /// Gets or sets whether request IP addresses are masked before being sent.
  /// IPv4 and IPv4-mapped IPv6 addresses are masked to the embedded IPv4 /24 prefix;
  /// native IPv6 addresses are masked to a /48 prefix. Known client-IP headers are excluded
  /// from request metadata while masking is enabled.
  /// </summary>
  public bool IsRequestIpAddressMasked { get; set; }

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
