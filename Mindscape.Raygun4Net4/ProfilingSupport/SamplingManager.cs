using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public enum DataSamplingMethod
  {
    None,
    Simple,
    Thumbprint
  }

  

  

  public interface IDataSampler
  {
    bool TakeSample(Uri uri);
  }

  public class SimpleRateSampler : IDataSampler
  {
    private int _count;
    private readonly int _take;
    private readonly int _limit;

    /// <summary>
    /// Constructor a SimpleRateSampler by actual values.
    /// </summary>
    /// <param name="take">A number to accept</param>
    /// <param name="limit">Out of a total number of traces</param>
    public SimpleRateSampler(int take, int limit)
    {
      if (take < 1)
      {
        take = 1;
      }
      
      if (limit < 1)
      {
        limit = 1;
      }
      
      if (take > limit)
      {
        take = limit;
      }

      _take = take;
      _limit = limit;
    }

    public bool TakeSample(Uri uri)
    {
      Reset();

      // Increment total seen
      _count++;

      return _count <= _take;
    }

    private void Reset()
    {
      // Reset if the count reaches the limit
      if (_count >= _limit)
      {
        _count = 0;
      }
    }
  }

  public class PerUriRateSampler : IDataSampler
  {
    private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets;
    private readonly int _maxPerInterval;
    private readonly TimeSpan _interval;

    public PerUriRateSampler(int maxPerInterval, TimeSpan interval)
    {
      _maxPerInterval = maxPerInterval;
      _interval = interval;
      _tokenBuckets = new ConcurrentDictionary<string, TokenBucket>();
    }

    public bool TakeSample(Uri uri)
    {
      var sample = _tokenBuckets.GetOrAdd(uri.AbsolutePath, CreateTokenBucket);
      return sample.Consume();
    }

    private TokenBucket CreateTokenBucket(string thumbprint)
    {
      return new TokenBucket(_maxPerInterval, _maxPerInterval, _interval);
    }
  }

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
