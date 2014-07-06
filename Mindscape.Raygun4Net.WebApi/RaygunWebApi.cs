using System;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;

namespace Mindscape.Raygun4Net.WebApi
{
  public static class RaygunWebApi
  {
    public static void Init(HttpConfiguration config, Func<RaygunWebApiClient> generateRaygunClient = null)
    {
      var clientCreator = new CanCreateRaygunClient(generateRaygunClient);

      config.Services.Add(typeof(IExceptionLogger), new RaygunWebApiExceptionLogger(clientCreator));

      config.Filters.Add(new RaygunWebApiExceptionFilter(clientCreator));
      config.Filters.Add(new RaygunWebApiActionFilter(clientCreator));

      var concreteActivator = config.Services.GetHttpControllerActivator();
      config.Services.Replace(typeof(IHttpControllerActivator), new RaygunWebApiExceptionHandlingControllerActivator(concreteActivator, clientCreator));

      config.Services.Replace(typeof(IHttpControllerSelector), new RaygunWebApiCustomControllerSelector(config, clientCreator));
      config.Services.Replace(typeof(IHttpActionSelector), new RaygunWebApiCustomControllerActionSelector(clientCreator));
    }
  }
}