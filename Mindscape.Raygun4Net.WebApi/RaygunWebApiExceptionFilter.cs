using System;
using System.Net;
using System.Web.Http.Filters;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionFilter : ActionFilterAttribute
  {
    private readonly Func<RaygunWebApiClient> _generateRaygunClient;

    internal RaygunWebApiExceptionFilter(Func<RaygunWebApiClient> generateRaygunClient)
    {
      _generateRaygunClient = generateRaygunClient;
    }

    public override void OnActionExecuted(HttpActionExecutedContext context)
    {
      base.OnActionExecuted(context);

      if ((int)context.Response.StatusCode >= 400)
      {
        var controllerName = context.ActionContext.ControllerContext.ControllerDescriptor.ControllerName;
        var actionName = context.ActionContext.ActionDescriptor.ActionName;
        GetClient().Send(
          new HttpException(
            context.Response.StatusCode,
            string.Format("{0} while handling URL {1} in {2}.{3}", context.Response.ReasonPhrase, context.Request.RequestUri, controllerName, actionName)));
      }
    }

    private RaygunWebApiClient GetClient()
    {
      return _generateRaygunClient == null ? new RaygunWebApiClient() : _generateRaygunClient();
    }
  }

  public class HttpException : Exception
  {
    public HttpStatusCode StatusCode { get; set; }

    public HttpException(HttpStatusCode statusCode, string message)
      : base(message)
    {
      StatusCode = statusCode;
    }
  }
}