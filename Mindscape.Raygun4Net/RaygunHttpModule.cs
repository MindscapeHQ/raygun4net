using System;
using System.Web;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    public void Init(HttpApplication context)
    {
      context.Error += SendError;
    }

    public void Dispose()
    {
    }

    private void SendError(object sender, EventArgs e)
    {
      var application = (HttpApplication)sender;

      var exception = application.Server.GetLastError();

      var raygunClient = new RaygunClient();

      raygunClient.Send(exception);
    }
  }
}