using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Breadcrumbs;

public class InMemoryBreadcrumbStorage : IRaygunBreadcrumbStorage
{
  private readonly List<RaygunBreadcrumb> _breadcrumbs;

  private const int MaxSize = 32;

  public InMemoryBreadcrumbStorage(List<RaygunBreadcrumb> breadcrumbs = null)
  {
    _breadcrumbs = breadcrumbs ?? new List<RaygunBreadcrumb>();
  }

  public void Store(RaygunBreadcrumb breadcrumb)
  {
    if (_breadcrumbs == null)
    {
      return;
    }

    if (_breadcrumbs.Count == MaxSize)
    {
      _breadcrumbs.RemoveAt(0);
    }
    
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

  public IList<RaygunBreadcrumb> ToList()
  {
    return _breadcrumbs;
  }
}