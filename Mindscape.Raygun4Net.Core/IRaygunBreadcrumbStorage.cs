using System.Collections.Generic;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunBreadcrumbStorage : IEnumerable<RaygunBreadcrumb>
  {
    void Store(RaygunBreadcrumb breadcrumb);

    void Clear();
  }
}
