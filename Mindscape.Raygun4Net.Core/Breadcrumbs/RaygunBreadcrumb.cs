using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Breadcrumbs;

namespace Mindscape.Raygun4Net
{
  public class RaygunBreadcrumb
  {
    public RaygunBreadcrumb()
    {
      Level = RaygunBreadcrumbLevel.Info;
      Type = nameof(RaygunBreadcrumbType.Manual);
      Timestamp = (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
    }

    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public string Message { get; set; }

    public string Category { get; set; }

    public RaygunBreadcrumbLevel Level { get; set; }

    // This is a string due to serialization of enums in SimpleJson to the numeric value.
    public string Type { get; set; }

    public IDictionary<string, object> CustomData { get => _customData ?? (_customData = new Dictionary<string, object>()); set => _customData = value; }
    private IDictionary<string, object> _customData;

    public long Timestamp { get; set; }

    public string ClassName { get; set; }

    public string MethodName { get; set; }

    public int? LineNumber { get; set; }
  }
}
