using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Breadcrumbs;

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
    _breadcrumbs?.Clear();
  }

  public int Size()
  {
    return _breadcrumbs?.Count ?? 0;
  }

  public void RemoveFirst()
  {
    if (_breadcrumbs.Count == 0)
    {
      return;
    }

    _breadcrumbs?.RemoveAt(0);
  }

  public IList<RaygunBreadcrumb> ToList()
  {
    return _breadcrumbs;
  }
}