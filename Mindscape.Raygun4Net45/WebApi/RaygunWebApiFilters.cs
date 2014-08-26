using System;
using System.Collections.Generic;
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
      _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).SendInBackground(context.Exception);
    }

#pragma warning disable 1998
    public override async Task OnExceptionAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
    {
      _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).SendInBackground(context.Exception);
    }
#pragma warning restore 1998
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
            context.Response.ReasonPhrase,
            string.Format("HTTP {0} returned while handling Request {2} {1}", (int)context.Response.StatusCode, context.Request.RequestUri, context.Request.Method));
        }
        catch (RaygunWebApiHttpException e)
        {
          _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).SendInBackground(e, null, new Dictionary<string, string> { { "ReasonCode", e.ReasonPhrase } });
        }
        catch (Exception e)
        {
          // This is here on the off chance that interacting with the context or HTTP Response throws an exception.
          _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).SendInBackground(e);
        }
      }
    }
  }

  public class RaygunWebApiHttpException : Exception
  {
    public HttpStatusCode StatusCode { get; set; }

    public string ReasonPhrase { get; set; }

    public RaygunWebApiHttpException(HttpStatusCode statusCode, string reasonPhrase, string message)
      : base(message)
    {
      ReasonPhrase = reasonPhrase;
      StatusCode = statusCode;
    }
  }
}