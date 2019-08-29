using System;
using System.Collections.Generic;
using System.Linq;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public static class SettingsManager
  {
    //This method will not parse the entire JSON structure, it only looks at the following fields:
    //{
    //  ...
    //  "ApiKeyConfiguration": [
    //    {
    //      ...
    //      "SamplingMethod": 2,
    //      "SamplingConfig": "{\"SampleAmount\":7,\"SampleIntervalAmount\":2,\"SampleIntervalOption\":2}",
    //      "SamplingOverrides": [
    //        {
    //          "Type": 0,
    //          "OverrideData": "{\"SampleAmount\":5,\"SampleBucketSize\":10,\"SampleOption\":\"Traces\",\"Url\":\"test-traces.com\"}"
    //        },
    //        ...
    //      ]
    //    }
    //  ]
    //}
    public static SamplingSetting ParseSamplingSettings(string settingsText, string identifier)
    {
      var settings = SimpleJson.DeserializeObject(settingsText) as JsonObject;      
      if (settings == null)
      {
        System.Diagnostics.Trace.WriteLine("Invalid Json was provided: " + settingsText);
        return null;
      }

      if (String.IsNullOrEmpty(identifier))
      {
        System.Diagnostics.Trace.WriteLine($"A site name must be provided for {nameof(identifier)}");
        return null;
      }

      if (settings.ContainsKey("ApiKeyConfiguration") == false)
      {
        System.Diagnostics.Trace.WriteLine($"Expected property \"ApiKeyConfiguration\" in the JSON values: {settingsText}");
        return null;
      }

      var siteSettings = settings["ApiKeyConfiguration"] as JsonArray;
      foreach (JsonObject siteSetting in siteSettings)
      {
        var expectedKeys = new[] { "Identifier", "SamplingMethod", "SamplingConfig", "SamplingOverrides" };
        if (expectedKeys.Any(key => siteSetting.ContainsKey(key) == false))
        {
          System.Diagnostics.Trace.WriteLine($"Expected all the following properties {string.Join(", ", expectedKeys)} in the JSON values: {settingsText}");
          continue;
        }

        if ((string)siteSetting["Identifier"] != identifier)
        {
          continue;
        }

        var samplingMethod = (DataSamplingMethod)(long)siteSetting["SamplingMethod"];
        var policy = new SamplingPolicy(samplingMethod, (string)siteSetting["SamplingConfig"]);
        var overrides = new List<UrlSamplingOverride>();
        var overrideJsonArray = (JsonArray)siteSetting["SamplingOverrides"];

        if (overrideJsonArray == null || overrideJsonArray.Count == 0)
        {
          return new SamplingSetting(policy, overrides);
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
              if (overrideSettingConfiguration.ContainsKey("Url") == false || 
                  overrideSettingConfiguration.ContainsKey("SampleOption") == false)
              {
                System.Diagnostics.Trace.WriteLine($"Expected properties \"Url\" and \"SampleOption\" in the JSON values: {overrideSettingConfiguration}");
                continue;
              }

              var overrideUrl = (string)overrideSettingConfiguration["Url"];
              var overridePolicyTypeRaw = (string)overrideSettingConfiguration["SampleOption"];
              var overridePolicyType = (SamplingOption)Enum.Parse(typeof(SamplingOption), overridePolicyTypeRaw);
              var dataSamplingMethod = overridePolicyType == SamplingOption.Traces ? DataSamplingMethod.Simple : DataSamplingMethod.Thumbprint;

              var overridePolicy = new SamplingPolicy(dataSamplingMethod, overrideConfigurationData);
              var samplingOverride = new UrlSamplingOverride(overrideUrl, overridePolicy);
              overrides.Add(samplingOverride);
            }
          }
        }

        return new SamplingSetting(policy, overrides);
      }

      return null;
    }
  }
}
