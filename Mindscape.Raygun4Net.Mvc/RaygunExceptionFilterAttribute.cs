using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Mindscape.Raygun4Net
{
  public class RaygunExceptionFilterAttribute : FilterAttribute, IExceptionFilter
  {
    private HttpApplication _application;
    private RaygunHttpModule _httpModeule;

    public RaygunExceptionFilterAttribute(HttpApplication application, RaygunHttpModule httpModule)
    {
      _application = application;
      _httpModeule = httpModule;
    }

    public void OnException(ExceptionContext filterContext)
    {
      _httpModeule.SendError(_application, filterContext.Exception);
    }
  }
}
