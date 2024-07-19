using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    private RaygunClient _raygunClient;

    private bool ExcludeErrorsBasedOnHttpStatusCode { get; set; }

    private bool ExcludeErrorsFromLocal { get; set; }

    private int[] HttpStatusCodesToExclude { get; set; }

    public void Init(HttpApplication context)
    {
      context.Error += SendError;

      HttpStatusCodesToExclude = RaygunSettings.Settings.ExcludedStatusCodes;
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Any();
      ExcludeErrorsFromLocal = RaygunSettings.Settings.ExcludeErrorsFromLocal;
      
      // If no version is set, attempt to retrieve the version from the assembly.
      if (string.IsNullOrEmpty(RaygunSettings.Settings.ApplicationVersion))
      {
        var entryAssembly = GetWebEntryAssembly(context);
        if (entryAssembly != null)
        {
          RaygunSettings.Settings.ApplicationVersion = entryAssembly.GetName()?.Version?.ToString();
        }
      }

      var mvcAssembly = AppDomain.CurrentDomain?.GetAssemblies()?.FirstOrDefault(a => a.FullName.StartsWith("Mindscape.Raygun4Net.Mvc,"));
      if (mvcAssembly != null)
      {
        var type = mvcAssembly.GetType("Mindscape.Raygun4Net.RaygunExceptionFilterAttacher");
        var method = type.GetMethod("AttachExceptionFilter", BindingFlags.Static | BindingFlags.Public);
        method.Invoke(null, new object[] { context, this });
      }
    }

    public void Dispose()
    {
    }

    private void SendError(object sender, EventArgs e)
    {
      var application = (HttpApplication)sender;
      var lastError = application.Server.GetLastError();

      if (CanSend(lastError))
      {
        var client = GetRaygunClient(application);
        client.SendInBackground(lastError, new List<string> {RaygunClient.UnhandledExceptionTag});
      }
    }

    public void SendError(HttpApplication application, Exception exception)
    {
      if (CanSend(exception))
      {
        var client = GetRaygunClient(application);
        client.SendInBackground(exception, new List<string> { RaygunClient.UnhandledExceptionTag });
      }
    }

    protected RaygunClient GetRaygunClient(HttpApplication application)
    {
      if (_raygunClient == null)
      {
        _raygunClient = application is IRaygunApplication raygunApplication
          ? raygunApplication.GenerateRaygunClient()
          : GenerateDefaultRaygunClient();
      }

      return _raygunClient;
    }

    private static RaygunClient GenerateDefaultRaygunClient()
    {
      var instance = new RaygunClient();

      if (HttpContext.Current != null && HttpContext.Current.Session != null)
      {
        instance.ContextId = HttpContext.Current.Session.SessionID;
      }

      return instance;
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

    private Assembly GetWebEntryAssembly(HttpApplication application)
    {
      var type = application?.GetType();
      
      while (type != null && "ASP".Equals(type.Namespace))
      {
        type = type.BaseType;
      }

      return type == null ? null : type.Assembly;
    }
  }
}