using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  internal class AspNetCoreInitializer
  {
    private static readonly TimeSpan AgentPollingDelay = new TimeSpan(0, 10, 0);
    private static readonly object Lock = new object();
    private ISamplingManager _samplingManager;
    private Timer _refreshTimer;
    private string _appIdentifier;

    internal void Initialize(string appIdentifier = null)
    {
      // In .NET Core, app identifier for sampling needs to be the the name of the main dll, i.e. MyApp.dll
      _appIdentifier = !string.IsNullOrWhiteSpace(appIdentifier) ? appIdentifier : Path.GetFileName(Assembly.GetEntryAssembly().Location);
      
      InitProfilingSupport();
    }
    
    private void InitProfilingSupport()
    {
      try
      {
        lock (Lock)
        {
          if (_samplingManager == null)
          {
            if (APM.ProfilerAttached)
            {
              System.Diagnostics.Debug.WriteLine("Detected Raygun APM profiler is attached, initializing profiler support.");

              _samplingManager = new SamplingManager();
              _refreshTimer = new Timer(RefreshAgentSettings, null, TimeSpan.Zero, AgentPollingDelay);
            }
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error initialising APM profiler support: {ex.Message}");
      }
    }

    internal async Task WrapAndInvokeRequest(RequestDelegate request, HttpContext httpContext)
    {
      BeginRequest(httpContext.Request);
      await request.Invoke(httpContext);
      EndRequest();
    }

    private void BeginRequest(HttpRequest request)
    {
      try
      {
        if (_samplingManager != null)
        {
          string url = $"{request.Scheme}://{request.Host}{request.Path}";
          if (!_samplingManager.TakeSample(url))
          {
            APM.Disable();
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error during begin request of APM sampling: {ex.Message}");
      }
    }

    private void EndRequest()
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
        System.Diagnostics.Debug.WriteLine($"Error during end request of APM sampling: {ex.Message}");
      }
    }

    private static readonly string SettingsFilePath = Path.Combine(
#if NETSTANDARD1_6
      @"C:\ProgramData\Raygun\AgentSettings\", // the best we can do under .NET Std 1.6|
#else
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
#endif
      "Raygun", "AgentSettings", "agent-configuration.json");

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
            System.Diagnostics.Debug.WriteLine($"Could not locate sampling settings for site {_appIdentifier}");
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error refreshing agent settings: {ex.Message}");
      }
    }
  }
}