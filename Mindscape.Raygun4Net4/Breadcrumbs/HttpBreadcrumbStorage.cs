using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace Mindscape.Raygun4Net.Breadcrumbs
{
  internal class HttpBreadcrumbStorage : IRaygunBreadcrumbStorage
  {
    private const string ItemsKey = "Raygun.Breadcrumbs.Storage";

    public IEnumerator<RaygunBreadcrumb> GetEnumerator()
    {
      return GetList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public void Store(RaygunBreadcrumb breadcrumb)
    {
      GetList().Add(breadcrumb);
    }

    public void Clear()
    {
      GetList().Clear();
    }

    private List<RaygunBreadcrumb> GetList()
    {
      var httpContext = HttpContext.Current;
      if (httpContext == null)
      {
        return new List<RaygunBreadcrumb>();
      }

      if (!httpContext.Items.Contains(ItemsKey))
      {
        httpContext.Items[ItemsKey] = new List<RaygunBreadcrumb>();
      }

      return (List<RaygunBreadcrumb>)httpContext.Items[ItemsKey];
    }
  }
}
