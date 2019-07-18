using System.Collections.Generic;

namespace System.Collections.Concurrent
{
  public delegate TResult Func<in T, out TResult>(T arg);

  /// <summary>
  /// This class implements just enough of the surface-area of ConcurrentDictionary to support our use-case
  /// (used when we are building pre .NET 4.0, e.g. .NET 3.5, (on .NET 4.0 we can use the built-in ConcurrentDictionary<,>)
  /// </summary>
  public class ConcurrentDictionary<TKey, TValue>
  {
    private readonly Dictionary<TKey, TValue> internalDictionary = new Dictionary<TKey, TValue>();
    private readonly object dictionaryLock = new object();

    // Adds a key/value pair to the ConcurrentDictionary<TKey,TValue> if the key does not already exist. 
    // Returns the new value, or the existing value if the key already exists.
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
      lock (dictionaryLock)
      {
        TValue existingValue;
        if (internalDictionary.TryGetValue(key, out existingValue))
        {
          return existingValue;
        }
        else
        {
          TValue newValue = valueFactory(key);
          internalDictionary.Add(key, newValue);
          return newValue;
        }
      }
    }
  }
}
