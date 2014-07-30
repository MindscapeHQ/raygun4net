using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionFilter : ExceptionFilterAttribute
  {
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiExceptionFilter(IRaygunWebApiClientProvider clientCreator)
    {
      _clientCreator = clientCreator;
    }

    public override void OnException(HttpActionExecutedContext context)
    {
      _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).Send(context.Exception);
    }

    public override Task OnExceptionAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
    {
      return Task.Factory.StartNew(() => _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).Send(context.Exception), cancellationToken);
    }
  }

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

      if (context != null && context.Response != null && (int)context.Response.StatusCode >= 400)
      {
        try
        {
          throw new RaygunWebApiHttpException(
            context.Response.StatusCode,
            string.Format("HTTP {0} returned while handling URL {1}", (int)context.Response.StatusCode, context.Request.RequestUri));
        }
        catch (Exception e)
        {
          _clientCreator.GenerateRaygunWebApiClient().Send(e);
        }
      }
    }
  }

  public class RaygunWebApiHttpException : Exception
  {
    public HttpStatusCode StatusCode { get; set; }

    public RaygunWebApiHttpException(HttpStatusCode statusCode, string message)
      : base(message)
    {
      StatusCode = statusCode;
    }
  }
}