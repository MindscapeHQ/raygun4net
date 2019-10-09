using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class TokenBucket
  {
    private DateTime _nextRefill;
    private readonly TimeSpan _refillInterval;
    private readonly int _refillAmount;

    public TokenBucket(int capacity, int refillAmount, TimeSpan refillInterval)
    {
      _refillAmount = refillAmount;
      _refillInterval = refillInterval;
      _nextRefill = DateTime.UtcNow.Add(refillInterval);

      Capacity = capacity;
      Count = capacity;
    }

    public int Capacity { get; private set; }
    public int Count { get; private set; }

    public bool Consume(int amount = 1)
    {
      Refill();

      if (amount <= Count)
      {
        Count -= amount;
        return true;
      }

      return false;
    }

    private void Refill()
    {
      if (DateTime.UtcNow < _nextRefill)
      {
        return;
      }

      int newTokens = Math.Min(Capacity, Math.Max(0, _refillAmount));

      // Fast-forward to most recent interval
      while (_nextRefill.Add(_refillInterval) < DateTime.UtcNow)
      {
        RefillByAmount(newTokens);
      }

      RefillByAmount(newTokens);
    }

    private void RefillByAmount(int newTokens)
    {
      Count = Math.Max(0, Math.Min(Count + newTokens, Capacity));
      _nextRefill = _nextRefill.Add(_refillInterval);
    }
  }
}
