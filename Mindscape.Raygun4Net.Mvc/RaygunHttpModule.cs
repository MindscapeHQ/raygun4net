using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    private bool ExcludeErrorsBasedOnHttpStatusCode { get; set; }
    private bool ExcludeErrorsFromLocal { get; set; }

    private int[] HttpStatusCodesToExclude { get; set; }

    public void Init(HttpApplication context)
    {
      HttpStatusCodesToExclude = string.IsNullOrEmpty(RaygunSettings.Settings.ExcludeHttpStatusCodesList) ? new int[0] : RaygunSettings.Settings.ExcludeHttpStatusCodesList.Split(',').Select(int.Parse).ToArray();
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Any();
      ExcludeErrorsFromLocal = RaygunSettings.Settings.ExcludeErrorsFromLocal;

      if (GlobalFilters.Filters.Count == 1)
      {
        Filter filter = GlobalFilters.Filters.FirstOrDefault();
        if (filter != null && filter.Instance.GetType().FullName.Equals("System.Web.Mvc.HandleErrorAttribute"))
        {
          GlobalFilters.Filters.Add(new RaygunExceptionFilterAttribute(context, this));
        }
      }

      context.Error += SendError;
    }

    public void Dispose()
    {
    }

    private void SendError(object sender, EventArgs e)
    {
      var application = (HttpApplication)sender;
      var lastError = application.Server.GetLastError();

      SendError(application, lastError);
    }

    internal void SendError(HttpApplication application, Exception exception)
    {
      if (CanSend(exception))
      {
        var client = GetRaygunClient(application);
        client.SendInBackground(Unwrap(exception));
      }
    }

    protected RaygunClient GetRaygunClient(HttpApplication application)
    {
      var raygunApplication = application as IRaygunApplication;
      return raygunApplication != null ? raygunApplication.GenerateRaygunClient() : new RaygunClient();
    }

    protected bool CanSend(Exception exception)
    {
      if (ExcludeErrorsBasedOnHttpStatusCode && exception is HttpException && HttpStatusCodesToExclude.Contains(((HttpException)exception).GetHttpCode()))
      {
        return false;
      }

      if (ExcludeErrorsFromLocal && HttpContext.Current.Request.IsLocal)
      {
        return false;
      }

      return true;
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
