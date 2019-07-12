using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using Mindscape.Raygun4Net.ProfilingSupport;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    private static TimeSpan AgentPollingDelay = new TimeSpan(0, 5, 0);
    private static ISamplingManager _samplingManager;

    private bool ExcludeErrorsBasedOnHttpStatusCode { get; set; }
    private bool ExcludeErrorsFromLocal { get; set; }

    private int[] HttpStatusCodesToExclude { get; set; }

    public void Init(HttpApplication context)
    {
      context.Error += SendError;
      context.BeginRequest += BeginRequest;
      context.EndRequest += EndRequest;

      HttpStatusCodesToExclude = new int[0];
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.ExcludeHttpStatusCodesList))
      {
        List<int> codes = new List<int>();
        string[] parts = RaygunSettings.Settings.ExcludeHttpStatusCodesList.Split(',');
        foreach(string code in parts)
        {
          int c;
          if (int.TryParse(code, out c))
          {
            codes.Add(c);
          }
        }
        HttpStatusCodesToExclude = codes.ToArray();
      }
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Length > 0;
      ExcludeErrorsFromLocal = RaygunSettings.Settings.ExcludeErrorsFromLocal;

      InitProfilingSupport();
    }

    private void BeginRequest(object sender, EventArgs e)
    {
      if (_samplingManager != null)
      {
        var application = (HttpApplication)sender;

        if (!_samplingManager.TakeSample(application.Request.Url))
        {
          APM.Disable();
        }
      }
    }

    private void EndRequest(object sender, EventArgs e)
    {
      if (_samplingManager != null)
      {
        APM.Enable();
      }
    }
    private void InitProfilingSupport()
    {
      if (APM.ProfilerAttached)
      {
        new Thread(new ThreadStart(RefreshAgentSettings)).Start();

        _samplingManager = new SamplingManager();
      }
    }

    private static string settingsFilePath =
      Path.Combine(
        Path.Combine(
          Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Raygun"),
          "AgentSettings"),
        "agent-configuration.json");

    public void RefreshAgentSettings()
    {
      while (true)
      {
        try
        {
          if (File.Exists(settingsFilePath))
          {
            var settingsText = File.ReadAllText(settingsFilePath);
            var siteName = System.Web.Hosting.HostingEnvironment.SiteName;

            var samplingSetting = SettingsManager.FetchSamplingSettings(settingsText, siteName);
            if (samplingSetting != null)
              _samplingManager.SetSamplingPolicy(samplingSetting.Policy, samplingSetting.Overrides);
          }
        }
        catch (ThreadAbortException)
        {
          return;
        }
        catch (Exception)
        {
        }

        Thread.Sleep(AgentPollingDelay);
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
        client.SendInBackground(lastError);
      }
    }

    protected RaygunClient GetRaygunClient(HttpApplication application)
    {
      var raygunApplication = application as IRaygunApplication;
      return raygunApplication != null ? raygunApplication.GenerateRaygunClient() : new RaygunClient();
    }

    protected bool CanSend(Exception exception)
    {
      if (ExcludeErrorsBasedOnHttpStatusCode && exception is HttpException && Contains(HttpStatusCodesToExclude, ((HttpException)exception).GetHttpCode()))
      {
        return false;
      }

      if (ExcludeErrorsFromLocal && HttpContext.Current.Request.IsLocal)
      {
        return false;
      }

      return true;
    }

    private static bool Contains(int[] array, int target)
    {
      foreach (int i in array)
      {
        if (i.Equals(target))
        {
          return true;
        }
      }
      return false;
    }
  }
}
