using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net
{
  public class RaygunBreadcrumb : IRaygunBreadcrumb
  {
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    public RaygunBreadcrumbLevel Level { get; set; } = RaygunBreadcrumbLevel.Info;

    public RaygunBreadcrumbType Type { get; set; } = RaygunBreadcrumbType.Manual;
    
    public string Message { get; set; }

    public string Category { get; set; }

    public IDictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

    public long Timestamp { get; set; } = (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;

    public string ClassName { get; set; }

    public string MethodName { get; set; }

    public int? LineNumber { get; set; }
  }
}