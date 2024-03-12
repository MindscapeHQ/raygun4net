using System.Collections.Generic;
using System.Threading;

namespace Mindscape.Raygun4Net.Breadcrumbs;

public class AsyncLocalBreadcrumbStorage : IContextAwareStorage
{
  private readonly AsyncLocal<List<RaygunBreadcrumb>> _breadcrumbs = new();

  public AsyncLocalBreadcrumbStorage(List<RaygunBreadcrumb> breadcrumbs = null)
  {
    _breadcrumbs.Value = breadcrumbs ?? new List<RaygunBreadcrumb>();
  }

  public void BeginContext()
  {
    _breadcrumbs.Value ??= new List<RaygunBreadcrumb>();
  }

  public void EndContext()
  {
    _breadcrumbs.Value = null;
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

  public int Size()
  {
    return _breadcrumbs.Value?.Count ?? 0;
  }

  public void RemoveFirst()
  {
    if (_breadcrumbs.Value == null)
    {
      return;
    }

    if (_breadcrumbs.Value.Count == 0)
    {
      return;
    }

    _breadcrumbs.Value.RemoveAt(0);
  }

  public IList<RaygunBreadcrumb> ToList()
  {
    return _breadcrumbs.Value;
  }
}