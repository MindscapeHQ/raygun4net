using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinEnvironment = System.Collections.Generic.IDictionary<string, object>;

namespace Mindscape.Raygun4Net.Owin.Middleware
{
  public class RaygunOwinExceptionMiddleware : OwinMiddleware
  {
    private readonly RaygunOwinClient _client;

    public RaygunOwinExceptionMiddleware(OwinMiddleware next, RaygunOwinClient client) : base(next)
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

      return Next.Invoke(context).ContinueWith(appTask =>
      {
        if (appTask.IsFaulted && appTask.Exception != null)
        {
          _client.SendInBackground(appTask.Exception);
        }

        return appTask;
      });
    }
  }
}