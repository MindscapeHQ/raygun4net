using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunBreadcrumb
  {
    public string Message { get; set; }
    public string Category { get; set; }
    public RaygunBreadcrumbs.Level Level { get; set; }
    public IDictionary CustomData { get; set; }

    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public int LineNumber { get; set; }
  }
}
