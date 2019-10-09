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

    private const string ROOT_CONFIGURATION_PROPERTY_NAME = "ApiKeyConfiguration";
    private const string IDENTIFIER_PROPERTY_NAME = "Identifier";
    private const string SAMPLING_METHOD_PROPERTY_NAME = "SamplingMethod";
    private const string SAMPLING_CONFIG_PROPERTY_NAME = "SamplingConfig";
    private const string SAMPLING_OVERRIDES_PROPERTY_NAME = "SamplingOverrides";
    private const string OVERRIDE_TYPE_PROPERTY_NAME = "Type";
    private const string OVERRIDE_DATA_PROPERTY_NAME = "OverrideData";
    private const string OVERRIDE_URL_PROPERTY_NAME = "Url";
    private const string OVERRIDE_SAMPLE_OPTION_PROPERTY_NAME = "SampleOption";
    
    public static SamplingSetting ParseSamplingSettings(string settingsText, string identifier)
    {
      var settings = SimpleJson.DeserializeObject(settingsText) as JsonObject;      
      if (settings == null)
      {
        System.Diagnostics.Debug.WriteLine("Invalid Json was provided: " + settingsText);
        return null;
      }

      if (String.IsNullOrEmpty(identifier))
      {
        System.Diagnostics.Debug.WriteLine($"A site name must be provided for {nameof(identifier)}");
        return null;
      }

      if (settings.ContainsKey(ROOT_CONFIGURATION_PROPERTY_NAME) == false)
      {
        System.Diagnostics.Debug.WriteLine($"Expected property \"{ROOT_CONFIGURATION_PROPERTY_NAME}\" in the JSON values: {settingsText}");
        return null;
      }

      var siteSettings = settings[ROOT_CONFIGURATION_PROPERTY_NAME] as JsonArray;
      foreach (JsonObject siteSetting in siteSettings)
      {
        var expectedKeys = new[] { IDENTIFIER_PROPERTY_NAME, SAMPLING_METHOD_PROPERTY_NAME, SAMPLING_CONFIG_PROPERTY_NAME, SAMPLING_OVERRIDES_PROPERTY_NAME };
        var containsAllKeys = true;
        foreach (var expectedKey in expectedKeys)
        {
          containsAllKeys = siteSetting.ContainsKey(expectedKey);
          if (!containsAllKeys)
          {
            System.Diagnostics.Debug.WriteLine($"Expected property {expectedKey} in the JSON values: {settingsText}");
            break;
          }
        }

        if (!containsAllKeys || (string)siteSetting[IDENTIFIER_PROPERTY_NAME] != identifier)
        {
          continue;
        }

        var samplingMethod = (DataSamplingMethod)(long)siteSetting[SAMPLING_METHOD_PROPERTY_NAME];
        var policy = new SamplingPolicy(samplingMethod, (string)siteSetting[SAMPLING_CONFIG_PROPERTY_NAME]);
        var overrides = new List<UrlSamplingOverride>();
        var overrideJsonArray = (JsonArray)siteSetting[SAMPLING_OVERRIDES_PROPERTY_NAME];

        if (overrideJsonArray == null || overrideJsonArray.Count == 0)
        {
          return new SamplingSetting(policy, overrides);
        }

        foreach (JsonObject overrideSetting in overrideJsonArray)
        {
          var overrideType = (int)Convert.ChangeType(overrideSetting[OVERRIDE_TYPE_PROPERTY_NAME], typeof(int));

          // Type 0: URL overrides
          if (overrideType == 0)
          {
            var overrideConfigurationData = (string)overrideSetting[OVERRIDE_DATA_PROPERTY_NAME];
            var overrideSettingConfiguration = SimpleJson.DeserializeObject(overrideConfigurationData) as JsonObject;

            if (overrideSettingConfiguration != null)
            {
              if (overrideSettingConfiguration.ContainsKey(OVERRIDE_URL_PROPERTY_NAME) == false || 
                  overrideSettingConfiguration.ContainsKey(OVERRIDE_SAMPLE_OPTION_PROPERTY_NAME) == false)
              {
                System.Diagnostics.Debug.WriteLine($"Expected properties \"{OVERRIDE_URL_PROPERTY_NAME}\" and \"{OVERRIDE_SAMPLE_OPTION_PROPERTY_NAME}\" in the JSON values: {overrideSettingConfiguration}");
                continue;
              }

              var overrideUrl = (string)overrideSettingConfiguration[OVERRIDE_URL_PROPERTY_NAME];
              var overridePolicyTypeRaw = (string)overrideSettingConfiguration[OVERRIDE_SAMPLE_OPTION_PROPERTY_NAME];
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
