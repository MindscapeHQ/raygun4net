using System;
using System.IO;
using System.Threading;
using System.Web;
using Mindscape.Raygun4Net.Logging;
using Mindscape.Raygun4Net.ProfilingSupport;

namespace Mindscape.Raygun4Net
{
  public class HttpApplicationInitializer
  {
    private static readonly TimeSpan AgentPollingDelay = new TimeSpan(0, 10, 0);
    private static readonly object Lock = new object();
    private static ISamplingManager _samplingManager;
    private static Timer _refreshTimer;
    private string _appIdentifier;

    public void Initialize(HttpApplication context, string appIdentifier = null)
    {
      _appIdentifier = !string.IsNullOrEmpty(appIdentifier) ? appIdentifier : System.Web.Hosting.HostingEnvironment.SiteName;

      if (InitProfilingSupport())
      {
        context.BeginRequest += BeginRequest;
        context.EndRequest += EndRequest;
      }
    }

    private void BeginRequest(object sender, EventArgs e)
    {
      try
      {
        if (_samplingManager != null)
        {
          var application = (HttpApplication) sender;

          var urlExcludingQuery = application.Request.Url.GetLeftPart(UriPartial.Path);
          if (!_samplingManager.TakeSample(urlExcludingQuery))
          {
            APM.Disable();
          }
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Error during begin request of APM sampling: {ex.Message}");
      }
    }

    private void EndRequest(object sender, EventArgs e)
    {
      try
      {
        if (_samplingManager != null)
        {
          APM.Enable();
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Error during end request of APM sampling: {ex.Message}");
      }
    }

    private bool InitProfilingSupport()
    {
      try
      {
        lock (Lock)
        {
          if (_samplingManager == null)
          {
            if (APM.ProfilerAttached)
            {
              RaygunLogger.Instance.Info("Detected Raygun APM profiler is attached, initializing profiler support.");

              _samplingManager = new SamplingManager();
              _refreshTimer = new Timer(RefreshAgentSettings, null, TimeSpan.Zero, AgentPollingDelay);

              return true;
            }
          }
          else
          {
            return true;
          }
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Error initialising APM profiler support: {ex.Message}");
      }

      return false;
    }

    private static readonly string SettingsFilePath =
      Path.Combine(
        Path.Combine(
          Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Raygun"),
          "AgentSettings"),
        "agent-configuration.json");

    private void RefreshAgentSettings(object state)
    {
      try
      {
        if (File.Exists(SettingsFilePath))
        {
          var settingsText = File.ReadAllText(SettingsFilePath);
          var samplingSetting = SettingsManager.ParseSamplingSettings(settingsText, _appIdentifier);
          if (samplingSetting != null)
          {
            _samplingManager.SetSamplingPolicy(samplingSetting.Policy, samplingSetting.Overrides);
          }
          else
          {
            RaygunLogger.Instance.Warning($"Could not locate sampling settings for site {_appIdentifier}");
          }
        }
        else
        {
          RaygunLogger.Instance.Warning($"Could not locate Raygun APM configuration file {SettingsFilePath}");
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Error refreshing agent settings: {ex.Message}");
      }
    }
  }
}