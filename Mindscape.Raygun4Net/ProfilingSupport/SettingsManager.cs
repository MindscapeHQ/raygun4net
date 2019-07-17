using System;
using System.Collections.Generic;

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
        throw new InvalidOperationException("InvalidJson was provided: " + settingsText);
      }

      if (String.IsNullOrEmpty(identifier))
      {
        throw new ArgumentException("A site name must be provided", nameof(identifier));
      }

      if (settings.ContainsKey("ApiKeyConfiguration") == false)
      {
        throw new InvalidOperationException(
          string.Format("Expected property \"ApiKeyConfiguration\" in the JSON values: {0}", settingsText));
      }

      var siteSettings = settings["ApiKeyConfiguration"] as JsonArray;
      foreach (JsonObject siteSetting in siteSettings)
      {
        var expectedKeys = new[] { "Identifier", "SamplingMethod", "SamplingConfig", "SamplingOverrides" };
        foreach (var expectedKey in expectedKeys)
        {
          if (siteSetting.ContainsKey(expectedKey) == false)
          {
            throw new InvalidOperationException(
              string.Format("Expected all the following properties {0} in the JSON values: {1}", String.Join(", ", expectedKeys), settingsText));
          }
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
                throw new InvalidOperationException(
                  string.Format("Expected properties \"Url\" and \"SampleOption\" in the JSON values: {0}", overrideSettingConfiguration));
              }

              var overrideUrl = (string)overrideSettingConfiguration["Url"];
              var overridePolicyTypeRaw = (string)overrideSettingConfiguration["SampleOption"];
              var overridePolicyType = (SamplingOption)Enum.Parse(typeof(SamplingOption), overridePolicyTypeRaw);
              var dataSamplingMethod =
                overridePolicyType == SamplingOption.Traces ? DataSamplingMethod.Simple : DataSamplingMethod.Thumbprint;

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
