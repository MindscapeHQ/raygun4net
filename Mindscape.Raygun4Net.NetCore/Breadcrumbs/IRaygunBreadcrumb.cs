using System.Collections.Generic;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunBreadcrumb
  {
    RaygunBreadcrumbLevel Level { get; set; }

    RaygunBreadcrumbType Type { get; set; }
    
    string Message { get; set; }
  
    string Category { get; set; }

    IDictionary<string, object> CustomData { get; set; }

    long Timestamp { get; set; }

    string ClassName { get; set; }

    string MethodName { get; set; }

    int? LineNumber { get; set; }
  }
}