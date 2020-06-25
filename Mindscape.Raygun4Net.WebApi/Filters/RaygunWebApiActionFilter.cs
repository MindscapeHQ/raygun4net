using System.Collections.Generic;
using System.Web.Http.Filters;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiActionFilter : ActionFilterAttribute
  {
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiActionFilter(IRaygunWebApiClientProvider clientCreator)
    {
      _clientCreator = clientCreator;
    }

    public override void OnActionExecuted(HttpActionExecutedContext context)
    {
      base.OnActionExecuted(context);

      // Don't bother processing bad StatusCodes if there is an exception attached - it will be handled by another part of the framework.
      if (context != null && context.Exception == null && context.Response != null && (int)context.Response.StatusCode >= 400)
      {
        var e = new RaygunWebApiHttpException(
          $"HTTP {(int)context.Response.StatusCode} returned while handling Request {context.Request.Method} {context.Request.RequestUri}",
          context.Response);

        _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(e, new List<string> {RaygunWebApiClient.UnhandledExceptionTag});
      }
    }
  }
}