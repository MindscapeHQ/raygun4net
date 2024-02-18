using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Breadcrumbs
{
  public interface IRaygunBreadcrumbStorage
  {
    void Store(RaygunBreadcrumb breadcrumb);

    void Clear();

    int Size();

    IList<RaygunBreadcrumb> ToList();
  }
}
