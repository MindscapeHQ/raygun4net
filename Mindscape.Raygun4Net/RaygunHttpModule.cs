using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

      HttpStatusCodesToExclude = RaygunSettings.Settings.ExcludedStatusCodes;
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Any();
      ExcludeErrorsFromLocal = RaygunSettings.Settings.ExcludeErrorsFromLocal;

      var mvcAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith("Mindscape.Raygun4Net.Mvc,"));
      if (mvcAssembly != null)
      {
        var type = mvcAssembly.GetType("Mindscape.Raygun4Net.RaygunExceptionFilterAttacher");
        var method = type.GetMethod("AttachExceptionFilter", BindingFlags.Static | BindingFlags.Public);
        method.Invoke(null, new object[] { context, this });
      }

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
   
    public void RefreshAgentSettings()
    {
      while (true)
      {
        try
        {          
          var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Raygun", "AgentSettings", "agent-configuration.json");

          if (File.Exists(settingsFilePath))
          {
            var settingsText = File.ReadAllText(settingsFilePath);
            var siteName = System.Web.Hosting.HostingEnvironment.SiteName;
            var settings = SimpleJson.DeserializeObject(settingsText) as JsonObject;
            var siteSettings = settings["ApiKeyConfiguration"] as JsonArray;

            if (settings != null && !String.IsNullOrEmpty(siteName))
            {
              foreach (JsonObject siteSetting in siteSettings)
              {
                if ((string)siteSetting["Identifier"] == siteName)
                {
                  var samplingMethod = (DataSamplingMethod)(long)siteSetting["SamplingMethod"];
                  var policy = new SamplingPolicy(samplingMethod, (string)siteSetting["SamplingConfig"]);
                  var overrides = new List<UrlSamplingOverride>();
                  var overrideJsonArray = (JsonArray)siteSetting["SamplingOverrides"];

                  if (overrideJsonArray != null && overrideJsonArray.Count > 0)
                  {
                    foreach (JsonObject overrideSetting in overrideJsonArray)
                    {
                      var overrideType = (int)overrideSetting["Type"];

                      // Type 0: URL overrides
                      if (overrideType == 0)
                      {
                        var overrideConfigurationData = (string)overrideSetting["OverrideData"];
                        var overrideSettingConfiguration = SimpleJson.DeserializeObject(overrideConfigurationData) as JsonObject;

                        if (overrideSettingConfiguration != null)
                        {
                          var overrideUrl = (string)overrideSettingConfiguration["Url"];
                          var overridePolicyType = (SamplingOption)overrideSettingConfiguration["SampleIntervalOption"];
                          var overridePolicy = new SamplingPolicy(overridePolicyType == SamplingOption.Traces ? DataSamplingMethod.Simple : DataSamplingMethod.Thumbprint, overrideConfigurationData);
                          var samplingOverride = new UrlSamplingOverride(overrideUrl, overridePolicy);
                        }
                      }
                    }
                  }

                  _samplingManager.SetSamplingPolicy(policy, overrides);
                }
              }
            }
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
      var raygunApplication = application as IRaygunApplication;
      return raygunApplication != null ? raygunApplication.GenerateRaygunClient() : GenerateDefaultRaygunClient(application);
    }

    private RaygunClient GenerateDefaultRaygunClient(HttpApplication application)
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
  }
}
