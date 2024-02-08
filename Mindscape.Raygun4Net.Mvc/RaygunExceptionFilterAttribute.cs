using System.Web;
using System.Web.Mvc;

namespace Mindscape.Raygun4Net
{
  public class RaygunExceptionFilterAttribute : FilterAttribute, IExceptionFilter
  {
    private HttpApplication _application;
    private RaygunHttpModule _httpModule;

    public RaygunExceptionFilterAttribute(HttpApplication application, RaygunHttpModule httpModule)
    {
      _application = application;
      _httpModule = httpModule;
    }

    public void OnException(ExceptionContext filterContext)
    {
      _httpModule.SendError(_application, filterContext.Exception);
    }
  }
}
