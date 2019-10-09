using System.Threading;

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

    public bool TakeSample(string url)
    {
      Reset();

      // Increment total seen
      Interlocked.Increment(ref _count);

      return _count <= Take;
    }

    private void Reset()
    {
      // Reset if the count reaches the limit
      if (_count >= Limit)
      {
        Interlocked.Exchange(ref _count, 0);
      }
    }
  }
}
