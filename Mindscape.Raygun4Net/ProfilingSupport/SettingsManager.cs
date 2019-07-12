using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public static class SettingsManager
  {
    public static SamplingSetting FetchSamplingSettings(string settingsText, string siteName)
    {
      var settings = SimpleJson.DeserializeObject(settingsText) as JsonObject;
      var siteSettings = settings["ApiKeyConfiguration"] as JsonArray;

      if (settings == null || String.IsNullOrEmpty(siteName))
      {
        return null;
      }

      foreach (JsonObject siteSetting in siteSettings)
      {
        if ((string)siteSetting["Identifier"] != siteName)
        {
          continue;
        }

        var samplingMethod = (DataSamplingMethod)(long)siteSetting["SamplingMethod"];
        var policy = new SamplingPolicy(samplingMethod, (string)siteSetting["SamplingConfig"]);
        var overrides = new List<UrlSamplingOverride>();
        var overrideJsonArray = (JsonArray)siteSetting["SamplingOverrides"];

        if (overrideJsonArray == null || overrideJsonArray.Count == 0)
        {
          continue;
        }

        foreach (JsonObject overrideSetting in overrideJsonArray)
        {
          var overrideType = (int)Convert.ChangeType(overrideSetting["Type"], typeof(int));

          // Type 0: URL overrides
          if (overrideType == 0)
          {
            var overrideConfigurationData = (string)overrideSetting["OverrideData"];
            var overrideSettingConfiguration = SimpleJson.DeserializeObject(overrideConfigurationData) as JsonObject;

            if (overrideSettingConfiguration != null)
            {
              var overrideUrl = (string)overrideSettingConfiguration["Url"];
              var overridePolicyType = (SamplingOption)(long)overrideSettingConfiguration["SamplingOption"];
              var dataSamplingMethod =
                overridePolicyType == SamplingOption.Traces ? DataSamplingMethod.Simple : DataSamplingMethod.Thumbprint;

              var overridePolicy = new SamplingPolicy(dataSamplingMethod, overrideConfigurationData);
              var samplingOverride = new UrlSamplingOverride(overrideUrl, overridePolicy);
              overrides.Add(samplingOverride);
            }
          }

          return new SamplingSetting(policy, overrides);
        }
      }

      return null;
    }
  }
}
