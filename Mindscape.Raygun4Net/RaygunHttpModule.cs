using System;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.SessionState;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    private bool ExcludeErrorsBasedOnHttpStatusCode { get; set; }    
    private int[] HttpStatusCodesToExclude { get; set; }    

    public void Init(HttpApplication context)
    {
      context.Error += SendError;
      context.AuthenticateRequest += AuthenticateRequest;
      context.PostAcquireRequestState += PostAcquireRequestState;
      context.BeginRequest += BeginRequest;
      context.EndRequest += EndRequest;

      var sessionModule = context.Modules["Session"] as SessionStateModule;
      sessionModule.Start += BeginSession;
      sessionModule.End += EndSession;

      new Thread(new ThreadStart(SendEvents)).Start();

      HttpStatusCodesToExclude = string.IsNullOrEmpty(RaygunSettings.Settings.ExcludeHttpStatusCodesList) ? new int[0] : RaygunSettings.Settings.ExcludeHttpStatusCodesList.Split(',').Select(int.Parse).ToArray();
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Any();
    }    

    private void BeginRequest(object sender, EventArgs e)
    {
      if (HttpContext.Current != null && HttpContext.Current.Request != null)
      {
        RaygunClient.Current.Source = 
          String.IsNullOrEmpty(HttpContext.Current.Request.ServerVariables["X_FORWARDED_FOR"]) ?
                    HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"] :
                    HttpContext.Current.Request.ServerVariables["X_FORWARDED_FOR"];
      }

      RaygunClient.Current.Raise("request-start");
    }

    private void EndRequest(object sender, EventArgs e)
    {
      RaygunClient.Current.Raise("request-end");
    }

    void SendEvents()
    {
      while (true)
      {
        try
        {
          RaygunClient.Current.SendEvents();
          Thread.Sleep(new TimeSpan(0, 1, 0));
        }
        catch (Exception ex)
        {
          RaygunClient.Current.Send(ex);
        }
      }
    }

    void PostAcquireRequestState(object sender, EventArgs e)
    {
      if (HttpContext.Current != null && HttpContext.Current.Session != null)
      {
        RaygunClient.Current.Context = HttpContext.Current.Session.SessionID;
      }
    }

    void AuthenticateRequest(object sender, EventArgs e)
    {
      if (HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
      {
        RaygunClient.Current.User = HttpContext.Current.User.Identity.Name;
      }
    }

    void BeginSession(object sender, EventArgs e)
    {
      RaygunClient.Current.Raise("session-start");
    }

    void EndSession(object sender, EventArgs e)
    {
      RaygunClient.Current.Raise("session-end");
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
