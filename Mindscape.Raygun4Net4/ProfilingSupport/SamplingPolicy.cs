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
            var amount = GetSamplingSetting<int>(configuration, "SampleAmount");
            var bucketSize = GetSamplingSetting<int>(configuration, "SampleBucketSize");

            Sampler = new SimpleRateSampler(amount ?? 1, bucketSize ?? 1);
          }
          break;
        case DataSamplingMethod.Thumbprint:
          {
            var amount = GetSamplingSetting<int>(configuration, "SampleAmount");
            var intervalOption = GetSamplingSetting<double>(configuration, "SampleIntervalOption");
            
            TimeSpan interval;

            switch ((SamplingOption)intervalOption)
            {
              case SamplingOption.Seconds:
                interval = TimeSpan.FromSeconds(intervalOption ?? 1);
                break;
              case SamplingOption.Hours:
                interval = TimeSpan.FromHours(intervalOption ?? 1);
                break;
              default:
                interval = TimeSpan.FromMinutes(intervalOption ?? 1);
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
