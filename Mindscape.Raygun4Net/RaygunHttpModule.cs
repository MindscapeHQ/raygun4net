using System;
using System.Linq;
using System.Web;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    private bool ExcludeErrorsBasedOnHttpStatusCode { get; set; }

    private int[] HttpStatusCodesToExclude { get; set; }

    public void Init(HttpApplication context)
    {
      context.Error += SendError;
      HttpStatusCodesToExclude = string.IsNullOrEmpty(RaygunSettings.Settings.ExcludeHttpStatusCodesList) ? new int[0] : RaygunSettings.Settings.ExcludeHttpStatusCodesList.Split(',').Select(int.Parse).ToArray();
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Any();
    }

    public void Dispose()
    {
    }

    private void SendError(object sender, EventArgs e)
    {
      var application = (HttpApplication)sender;
      var lastError = application.Server.GetLastError();

      if (ExcludeErrorsBasedOnHttpStatusCode && lastError is HttpException && HttpStatusCodesToExclude.Contains(((HttpException)lastError).GetHttpCode()))
      {
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
