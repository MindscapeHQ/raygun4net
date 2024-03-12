using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Breadcrumbs;

namespace Mindscape.Raygun4Net
{
  public class RaygunBreadcrumb
  {
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    public string Message { get; set; }

    public string Category { get; set; }

    // This is a string due to serialization of enums in SimpleJson to the numeric value.
    public string Type { get; set; } = "manual";

    public IDictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

    public long Timestamp { get; set; } = (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;

    public string ClassName { get; set; }

    public string MethodName { get; set; }

    public int? LineNumber { get; set; }
  }
}
