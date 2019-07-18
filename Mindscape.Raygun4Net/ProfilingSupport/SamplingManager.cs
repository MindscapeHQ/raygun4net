using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SamplingManager : ISamplingManager
  {
    private SamplingPolicy _policy;
    private List<UrlSamplingOverride> _overrides;

    public void SetSamplingPolicy(SamplingPolicy policy, List<UrlSamplingOverride> overrides = null)
    {
      _policy = policy;
      _overrides = new List<UrlSamplingOverride>();
      
      if (overrides != null)
      {
        _overrides.AddRange(overrides);
      }
    }

    public void AddOverride(UrlSamplingOverride samplingOverride)
    {
      if (samplingOverride == null) return;

      _overrides.Add(samplingOverride);
    }

    public bool TakeSample(Uri uri)
    {
      if (_policy == null) return true;

      SamplingPolicy activePolicy = _policy;

      // Check overrides first
      foreach (var samplingOveride in _overrides)
      {
        if (uri.ToString().Equals(samplingOveride.Url, StringComparison.OrdinalIgnoreCase))
        {
          activePolicy = samplingOveride.Policy;
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
