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

            var amount = GetSamplingSetting<int>(json, "SampleAmount") ?? 1;
            var bucketSize = GetSamplingSetting<int>(json, "SampleBucketSize") ?? 5;

            Sampler = new SimpleRateSampler(amount, bucketSize);
          }
          break;
        
        case DataSamplingMethod.Thumbprint:
          {
            // From the 'default value ("SamplingMethod": 2, is parsed before this method)
            // "SamplingConfig": "{ "SampleAmount":1, "SampleIntervalAmount":5, "SampleIntervalOption":1 }",
            //    OR
            // From override data ("SampleOption":"Minutes" and "Url": ".." can be ignored, they're parsed before this method/0
            // "OverrideData": "{ "SampleAmount":1, "SampleBucketSize":5, "SampleOption":"Seconds", "Url":"test-seconds.com" }"

            var json = SimpleJson.DeserializeObject(configuration) as JsonObject;

            // This is consistent across 'default' and 'override' value
            var amount = GetSamplingSetting<int>(json, "SampleAmount") ?? 1;

            // Default to 1 trace per 1 minute
            int intervalAmount = 1;
            SamplingOption intervalOption = SamplingOption.Minutes;
            
            if (json != null && json.ContainsKey("SampleIntervalOption") && json.ContainsKey("SampleIntervalAmount")) // 'default'
            {
              var intervalOptionRaw = GetSamplingSetting<int>(json, "SampleIntervalOption") ?? 2;
              intervalOption = (SamplingOption)intervalOptionRaw;
              intervalAmount = GetSamplingSetting<int>(json, "SampleIntervalAmount") ?? 1;

            }
            else if (json != null && json.ContainsKey("SampleOption") && json.ContainsKey("SampleBucketSize")) // an 'override'
            {
              var intervalOptionRaw = (string)json["SampleOption"];
              intervalOption = (SamplingOption)Enum.Parse(typeof(SamplingOption), intervalOptionRaw);
              intervalAmount = GetSamplingSetting<int>(json, "SampleBucketSize") ?? 1;
            }

            TimeSpan interval;
            switch (intervalOption)
            {
              case SamplingOption.Seconds:
                interval = TimeSpan.FromSeconds(intervalAmount);
                break;
              case SamplingOption.Hours:
                interval = TimeSpan.FromHours(intervalAmount);
                break;
              case SamplingOption.Minutes:
              default:
                interval = TimeSpan.FromMinutes(intervalAmount);
                break;
            }

            Sampler = new PerUriRateSampler(amount, interval);
          }
          break;
      }
    }

    public DataSamplingMethod SamplingMethod { get; private set; }    
    public IDataSampler Sampler { get; private set; }
    public string Configuration { get; private set; }

    private T? GetSamplingSetting<T>(JsonObject json, string settingName) where T : struct
    {
      if (json == null)
      {
        return default(T?);
      }

      return (T)Convert.ChangeType(json[settingName], typeof(T));      
    }
  }  
}
