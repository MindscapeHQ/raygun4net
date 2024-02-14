using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Mindscape.Raygun4Net.Breadcrumbs;

public class AsyncLocalBreadcrumbStorage : IRaygunBreadcrumbStorage
{
  private readonly AsyncLocal<List<RaygunBreadcrumb>> _breadcrumbs = new();

  public AsyncLocalBreadcrumbStorage(List<RaygunBreadcrumb> breadcrumbs = null)
  {
    _breadcrumbs.Value = breadcrumbs ?? new List<RaygunBreadcrumb>();
  }

  public void Store(RaygunBreadcrumb breadcrumb)
  {
    _breadcrumbs.Value ??= new List<RaygunBreadcrumb>();

    _breadcrumbs.Value.Add(breadcrumb);
  }

  public void Clear()
  {
    _breadcrumbs.Value?.Clear();
  }

  public IEnumerator<RaygunBreadcrumb> GetEnumerator()
  {
    return _breadcrumbs.Value?.GetEnumerator() ?? Enumerable.Empty<RaygunBreadcrumb>().GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}