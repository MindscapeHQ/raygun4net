using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunBreadcrumbStorage : IEnumerable<RaygunBreadcrumb>
  {
    void Store(RaygunBreadcrumb breadcrumb);
    void Clear();
  }
}
