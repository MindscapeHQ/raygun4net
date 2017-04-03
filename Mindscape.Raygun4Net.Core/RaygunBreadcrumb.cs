using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunBreadcrumb
  {
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public string Message { get; set; }
    public string Category { get; set; }
    public RaygunBreadcrumbs.Level Level { get; set; } = RaygunBreadcrumbs.Level.Info;
    public IDictionary CustomData { get; set; }
    public long Timestamp { get; set; } = (long) (DateTime.UtcNow - UnixEpoch).TotalSeconds;

    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public int? LineNumber { get; set; }
  }
}
