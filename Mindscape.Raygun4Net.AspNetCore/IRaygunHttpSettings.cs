using System.Collections.Generic;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore;

public interface IRaygunHttpSettings
{
  List<string> IgnoreSensitiveFieldNames { get; }
  List<string> IgnoreQueryParameterNames { get; }
  List<string> IgnoreFormFieldNames { get; }
  List<string> IgnoreHeaderNames { get; }
  List<string> IgnoreCookieNames { get; }
  List<string> IgnoreServerVariableNames { get; }
  List<IRaygunDataFilter> RawDataFilters { get; }
    
  bool IsRawDataIgnored { get; }
  bool IsRawDataIgnoredWhenFilteringFailed { get; }
  bool UseXmlRawDataFilter { get; }
  bool UseKeyValuePairRawDataFilter { get; }
}