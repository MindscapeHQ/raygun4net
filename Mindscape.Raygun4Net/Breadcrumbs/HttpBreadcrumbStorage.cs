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
      if (HttpContext.Current == null)
      {
        return new List<RaygunBreadcrumb>();
      }

      SetupStorage();

      return (List<RaygunBreadcrumb>) HttpContext.Current.Items[ItemsKey];
    }

    private void SetupStorage()
    {
      if (HttpContext.Current != null && !HttpContext.Current.Items.Contains(ItemsKey))
      {
        HttpContext.Current.Items[ItemsKey] = new List<RaygunBreadcrumb>();
      }
    }
  }
}
