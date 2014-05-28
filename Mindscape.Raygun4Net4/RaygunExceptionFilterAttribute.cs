using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Mindscape.Raygun4Net
{
  public class RaygunExceptionFilterAttribute : FilterAttribute, IExceptionFilter
  {
    public void OnException(ExceptionContext filterContext)
    {
      RaygunClient client = new RaygunClient();
      client.SendInBackground(filterContext.Exception);
    }
  }
}
