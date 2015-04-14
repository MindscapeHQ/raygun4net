using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mindscape.Raygun4Net
{
  public static class RaygunExceptionFilterAttacher
  {
    public static void AttachExceptionFilter(HttpApplication context, RaygunHttpModule module)
    {
      Filter filter = GlobalFilters.Filters.FirstOrDefault(f => f.Instance.GetType().FullName.Equals("System.Web.Mvc.HandleErrorAttribute"));
      if (filter != null)
      {
        if (!GlobalFilters.Filters.Any(f => f.Instance.GetType() == typeof(RaygunExceptionFilterAttribute)))
        {
          GlobalFilters.Filters.Add(new RaygunExceptionFilterAttribute(context, module));
        }
      }
    }
  }
}
