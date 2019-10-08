using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SamplingPolicy
  {
    private const string SAMPLE_AMOUNT = "SampleAmount";
    private const string SAMPLE_OPTION = "SampleOption";
    private const string SAMPLE_BUCKET_SIZE = "SampleBucketSize";
    private const string SAMPLE_INTERVAL_OPTION = "SampleIntervalOption";
    private const string SAMPLE_INTERVAL_AMOUNT = "SampleIntervalAmount";
    private const int DEFAULT_AMOUNT = 1; // 1 trace
    private const int DEFAULT_BUCKET_SIZE = 5; // 1 in 5 traces
    private const int DEFAULT_INTERVAL_AMOUNT = 1; // 1 minute
    private const int DEFAULT_INTERVAL_OPTION = 2; // SamplingOption.Minutes
    
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

            var amount = GetSamplingSetting<int>(json, SAMPLE_AMOUNT) ?? DEFAULT_AMOUNT;
            var bucketSize = GetSamplingSetting<int>(json, SAMPLE_BUCKET_SIZE) ?? DEFAULT_BUCKET_SIZE;

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
            var amount = GetSamplingSetting<int>(json, SAMPLE_AMOUNT) ?? DEFAULT_AMOUNT;

            // Default to 1 trace per 1 minute
            int intervalAmount = DEFAULT_INTERVAL_AMOUNT;
            SamplingOption intervalOption = SamplingOption.Minutes;
            
            if (json != null && json.ContainsKey(SAMPLE_INTERVAL_OPTION) && json.ContainsKey(SAMPLE_INTERVAL_AMOUNT)) // 'default'
            {
              var intervalOptionRaw = GetSamplingSetting<int>(json, SAMPLE_INTERVAL_OPTION) ?? DEFAULT_INTERVAL_OPTION;
              intervalOption = (SamplingOption)intervalOptionRaw;
              intervalAmount = GetSamplingSetting<int>(json, SAMPLE_INTERVAL_AMOUNT) ?? DEFAULT_INTERVAL_AMOUNT;

            }
            else if (json != null && json.ContainsKey(SAMPLE_OPTION) && json.ContainsKey(SAMPLE_BUCKET_SIZE)) // an 'override'
            {
              var intervalOptionRaw = (string)json[SAMPLE_OPTION];
              intervalOption = (SamplingOption)Enum.Parse(typeof(SamplingOption), intervalOptionRaw);
              intervalAmount = GetSamplingSetting<int>(json, SAMPLE_BUCKET_SIZE) ?? DEFAULT_INTERVAL_AMOUNT;
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
