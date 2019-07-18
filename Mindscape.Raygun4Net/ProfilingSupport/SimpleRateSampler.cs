using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SimpleRateSampler : IDataSampler
  {
    private int _count;

    public int Take { get; private set; }
    public int Limit { get; private set; }

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

      Take = take;
      Limit = limit;
    }

    public bool TakeSample(Uri uri)
    {
      Reset();

      // Increment total seen
      // TODO does this need to be made thread-safe, i.e. Interlocked or similar?
      _count++;

      return _count <= Take;
    }

    private void Reset()
    {
      // Reset if the count reaches the limit
      // TODO does this need to be made thread-safe, i.e. Interlocked or similar?
      if (_count >= Limit)
      {
        _count = 0;
      }
    }
  }
}
