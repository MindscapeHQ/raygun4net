using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Breadcrumbs
{
  internal class InMemoryBreadcrumbStorage : IRaygunBreadcrumbStorage
  {
    private readonly List<RaygunBreadcrumb> _breadcrumbs;

    public InMemoryBreadcrumbStorage(List<RaygunBreadcrumb> breadcrumbs = null)
    {
      _breadcrumbs = breadcrumbs ?? new List<RaygunBreadcrumb>();
    }

    public void Store(RaygunBreadcrumb breadcrumb)
    {
      _breadcrumbs.Add(breadcrumb);
    }

    public void Clear()
    {
      _breadcrumbs.Clear();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<RaygunBreadcrumb> GetEnumerator()
    {
      return _breadcrumbs.GetEnumerator();
    }
  }
}