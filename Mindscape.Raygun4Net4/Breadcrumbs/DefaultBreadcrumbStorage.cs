using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace Mindscape.Raygun4Net.Breadcrumbs
{
  public class DefaultBreadcrumbStorage : IRaygunBreadcrumbStorage
  {
    private readonly IRaygunBreadcrumbStorage _internalStorage;

    public DefaultBreadcrumbStorage()
    {
      if (HttpContext.Current != null)
      {
        _internalStorage = new HttpBreadcrumbStorage();
      }
      else
      {
        _internalStorage = new InMemoryBreadcrumbStorage();
      }
    }

    public IEnumerator<RaygunBreadcrumb> GetEnumerator()
    {
      return _internalStorage.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public void Store(RaygunBreadcrumb breadcrumb)
    {
      _internalStorage.Store(breadcrumb);
    }

    public void Clear()
    {
      _internalStorage.Clear();
    }
  }
}