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
      new RaygunClient().SendInBackground(Unwrap(application.Server.GetLastError()));
    }

    private Exception Unwrap(Exception exception)
    {
      if (exception is HttpUnhandledException)
      {
        return exception.GetBaseException();
      }

      return exception;
    }
  }
}