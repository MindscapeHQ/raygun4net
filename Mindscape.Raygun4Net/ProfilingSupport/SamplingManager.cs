using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SamplingManager : ISamplingManager
  {
    public SamplingPolicy Policy { get; private set; }
    public List<UrlSamplingOverride> Overrides { get; private set; }

    public void SetSamplingPolicy(SamplingPolicy policy, List<UrlSamplingOverride> overrides = null)
    {
      Policy = policy;
      Overrides = new List<UrlSamplingOverride>();
      
      if (overrides != null)
      {
        Overrides.AddRange(overrides);
      }
    }

    public bool TakeSample(Uri uri)
    {
      if (Policy == null) return true;

      SamplingPolicy activePolicy = Policy;

      // Check overrides first
      foreach (var samplingOverride in Overrides)
      {
        // Use a case-insensitive 'contains', because 'samplingOverride.Url' is from user input,
        // i.e. we don't know if it has 'http://', ends with '/', etc, so we do the best we can
        var url = uri.ToString();
        if (url.Length >= samplingOverride.Url.Length && samplingOverride.Url.Length > 0 &&
            url.IndexOf(samplingOverride.Url, StringComparison.OrdinalIgnoreCase) >= 0)
        {
          activePolicy = samplingOverride.Policy;
          break;
        }
      }

      switch (activePolicy.SamplingMethod)
      {
        case DataSamplingMethod.Simple:
        case DataSamplingMethod.Thumbprint:
        {
          if (activePolicy.Sampler == null) return true;

          return activePolicy.Sampler.TakeSample(uri);
        }
      }

      return true;
    }
  }
}
