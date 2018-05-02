using System;
using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net
{
  internal class RaygunInMemoryBreadcrumbStorage : IRaygunBreadcrumbStorage
  {
    private List<RaygunBreadcrumb> _breadcrumbs;

    public RaygunInMemoryBreadcrumbStorage(List<RaygunBreadcrumb> breadcrumbs = null)
    {
      _breadcrumbs = breadcrumbs ?? new List<RaygunBreadcrumb>();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<RaygunBreadcrumb> GetEnumerator()
    {
      return _breadcrumbs.GetEnumerator();
    }

    public void Store(RaygunBreadcrumb breadcrumb)
    {
      _breadcrumbs.Add(breadcrumb);
    }

    public void Clear()
    {
      _breadcrumbs.Clear();
    } 
  }
}