using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SamplingPolicy
  {
    public SamplingPolicy(DataSamplingMethod samplingMethod, string configuration)
    {
      SamplingMethod = samplingMethod;
      Configuration = configuration;

      switch (SamplingMethod)
      {
        case DataSamplingMethod.Simple:
          {
            // From the 'default' value ("SamplingMethod": 1, is parsed before this method)
            // "SamplingConfig": "{ "SampleAmount":2, "SampleBucketSize":4 }"
            //    OR
            // From override data ("SampleOption":"Traces" and "Url": ".." can be ignored, they're parsed before this method)
            // "OverrideData": "{ "SampleAmount":5, "SampleBucketSize":10, "SampleOption":"Traces", "Url":"test-traces.com" }"

            var json = SimpleJson.DeserializeObject(configuration) as JsonObject;
            if (json.ContainsKey("SampleAmount") == false || json.ContainsKey("SampleBucketSize") == false)
            {
              throw new InvalidOperationException(
                "Expected \"SampleAmount\" and \"SampleBucketSize\" propertiesy in the JSON values for the policy: " + configuration);
            }

            var amount = GetSamplingSetting<int>(configuration, "SampleAmount");
            var bucketSize = GetSamplingSetting<int>(configuration, "SampleBucketSize");

            Sampler = new SimpleRateSampler(amount ?? 1, bucketSize ?? 1);
          }
          break;
        case DataSamplingMethod.Thumbprint:
          {
            // From the 'default value ("SamplingMethod": 2, is parsed before this method)
            // "SamplingConfig": "{ "SampleAmount":1, "SampleIntervalAmount":5, "SampleIntervalOption":1 }",
            //    OR
            // From override data ("SampleOption":"Minuntes" and "Url": ".." can be ignored, they're parsed before this method/0
            // "OverrideData": "{ "SampleAmount":1, "SampleBucketSize":5, "SampleOption":"Seconds", "Url":"test-seconds.com" }"

            var json = SimpleJson.DeserializeObject(configuration) as JsonObject;
            if (json.ContainsKey("SampleAmount") == false)
            {
              throw new InvalidOperationException("Expected \"SampleAmount\" property in the JSON values for the policy: " + configuration);
            }

            // This is consistent across 'default' and 'override' value
            var amount = GetSamplingSetting<int>(configuration, "SampleAmount");

            SamplingOption intervalOption = SamplingOption.Traces;
            int? intervalAmount;
            if (json.ContainsKey("SampleIntervalOption") && json.ContainsKey("SampleIntervalAmount")) // 'default'
            {
              var intervalOptionRaw = GetSamplingSetting<int>(configuration, "SampleIntervalOption");
              intervalOption = (SamplingOption)intervalOptionRaw;
              intervalAmount = GetSamplingSetting<int>(configuration, "SampleIntervalAmount");

            }
            else if (json.ContainsKey("SampleOption") && json.ContainsKey("SampleBucketSize")) // an 'override'
            {
              var intervalOptionRaw = (string)json["SampleOption"];
              intervalOption = (SamplingOption)Enum.Parse(typeof(SamplingOption), intervalOptionRaw);
              intervalAmount = GetSamplingSetting<int>(configuration, "SampleBucketSize");
            }
            else
            {
              throw new InvalidOperationException("Unexpected JSON values for a policy: " + configuration);
            }

            TimeSpan interval;
            switch (intervalOption)
            {
              case SamplingOption.Seconds:
                interval = TimeSpan.FromSeconds(intervalAmount ?? 1);
                break;
              case SamplingOption.Hours:
                interval = TimeSpan.FromHours(intervalAmount ?? 1);
                break;
              case SamplingOption.Minutes:
              default:
                interval = TimeSpan.FromMinutes(intervalAmount ?? 1);
                break;
            }

            Sampler = new PerUriRateSampler(amount ?? 1, interval);
          }
          break;
      }
    }

    public DataSamplingMethod SamplingMethod { get; private set; }    
    public IDataSampler Sampler { get; private set; }
    public string Configuration { get; private set; }

    private T? GetSamplingSetting<T>(string configuration, string settingName) where T : struct
    {
      var json = SimpleJson.DeserializeObject(configuration) as JsonObject;

      if (json == null)
      {
        return default(T?);
      }

      return (T)Convert.ChangeType(json[settingName], typeof(T));      
    }
  }  
}
