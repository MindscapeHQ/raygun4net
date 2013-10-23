using System;
using System.Net;
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
	    var lastError = application.Server.GetLastError();
	    
		if(RaygunSettings.Settings.Exclude404NotFound)
		{
			if (lastError is HttpException && ((HttpException) lastError).GetHttpCode() == (int)HttpStatusCode.NotFound)
				return;
		}

	    new RaygunClient().SendInBackground(Unwrap(lastError));
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