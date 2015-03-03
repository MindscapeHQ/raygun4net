using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mindscape.Raygun4Net
{
  public static class RaygunExceptionFilterAttacher
  {
    public static void AttachExceptionFilter(HttpApplication context, RaygunHttpModule module)
    {
      if (GlobalFilters.Filters.Count == 1)
      {
        Filter filter = GlobalFilters.Filters.FirstOrDefault();
        if (filter != null && filter.Instance.GetType().FullName.Equals("System.Web.Mvc.HandleErrorAttribute"))
        {
          GlobalFilters.Filters.Add(new RaygunExceptionFilterAttribute(context, module));
        }
      }
    }
  }
}
