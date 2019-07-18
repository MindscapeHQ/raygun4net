using System;
using System.Collections.Concurrent;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class PerUriRateSampler : IDataSampler
  {
    private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets;

    public TimeSpan Interval { get; private set; }
    public int MaxPerInterval { get; private set; }

    public PerUriRateSampler(int maxPerInterval, TimeSpan interval)
    {
      MaxPerInterval = maxPerInterval;
      Interval = interval;
      _tokenBuckets = new ConcurrentDictionary<string, TokenBucket>();
    }

    public bool TakeSample(Uri uri)
    {
      var sample = _tokenBuckets.GetOrAdd(uri.AbsolutePath, CreateTokenBucket);
      return sample.Consume();
    }

    private TokenBucket CreateTokenBucket(string thumbprint)
    {
      return new TokenBucket(MaxPerInterval, MaxPerInterval, Interval);
    }
  }
}
