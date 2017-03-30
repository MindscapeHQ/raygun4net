using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Core.Tests.Models
{
  public class InMemoryBreadcrumbStorage : IRaygunBreadcrumbStorage
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
