using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;
using OwinEnvironment = System.Collections.Generic.IDictionary<string, object>;

namespace Mindscape.Raygun4Net.Owin.Middleware
{
  public class RaygunOwinErrorCodeMiddleware : OwinMiddleware
  {
    private readonly RaygunOwinClient _client;

    public RaygunOwinErrorCodeMiddleware(OwinMiddleware next, RaygunOwinClient client) : base(next)
    {
      if (next == null)
      {
        throw new ArgumentNullException("next");
      }

      if (client == null)
      {
        throw new ArgumentNullException("client");
      }

      _client = client;
    }

    public override Task Invoke(IOwinContext context)
    {
      if (_client == null)
      {
        return Next.Invoke(context);
      }

      return Next.Invoke(context)
        .ContinueWith(appTask =>
        {
          if (appTask.IsFaulted) { return appTask; }
          var responseCode = context.Response.StatusCode;
          if (responseCode >= 400)
          {
            _client.SendInBackground(new UnhandledRequestException(responseCode, context.Response.ReasonPhrase,
              string.Format("HTTP {0} returned while handling Request {2} {1}", responseCode, context.Request.Uri, context.Request.Method)));
          }
          return appTask;
        });
    }
  }

  public class UnhandledRequestException : Exception
  {
    public int StatusCode { get; set; }

    public string ReasonPhrase { get; set; }

    public UnhandledRequestException(int statusCode, string reasonPhrase, string message)
      : base(message)
    {
      ReasonPhrase = reasonPhrase;
      StatusCode = statusCode;
    }
  }
}