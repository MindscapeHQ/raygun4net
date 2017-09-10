using System;
using System.Net;
using System.Net.Http;
using System.Text;
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
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception);
    }

#pragma warning disable 1998
    public override async Task OnExceptionAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
    {
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception);
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

      // Don't bother processing bad StatusCodes if there is an exception attached - it will be handled by another part of the framework.
      if (context != null && context.Exception == null && context.Response != null && (int)context.Response.StatusCode >= 400)
      {
        Exception e = new RaygunWebApiHttpException(
          string.Format("HTTP {0} returned while handling Request {2} {1}", (int)context.Response.StatusCode, context.Request.RequestUri, context.Request.Method),
          context.Response);

        _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(e);
      }
    }
  }

  public class RaygunWebApiHttpException : Exception
  {
    public RaygunWebApiHttpException(HttpStatusCode statusCode, string reasonPhrase, string message)
      : base(message)
    {
      ReasonPhrase = reasonPhrase;
      StatusCode = statusCode;
    }

    public RaygunWebApiHttpException(string message, HttpResponseMessage response) :
      base(message)
    {
      ReasonPhrase = response.ReasonPhrase;
      StatusCode = response.StatusCode;
      Content = RaygunSettings.Settings.IsResponseContentIgnored ? null : response.Content.ReadAsString();
    }

    public HttpStatusCode StatusCode { get; set; }

    public string ReasonPhrase { get; set; }

    public string Content { get; set; }
  }
}