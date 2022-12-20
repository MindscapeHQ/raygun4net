using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Raygun.Raygun4Net.Maui;

public class RingBuffer<T>
{
    private readonly int _size;
    private ConcurrentQueue<T> Queue { get; }

    public RingBuffer(int size = 10)
    {
        _size = size;
        Queue = new ConcurrentQueue<T>();
    }

    public void Add([DisallowNull] T item)
    {
        if (item == null){ throw new ArgumentNullException(nameof(item));}

        while (Queue.Count >= _size)
        {
            Queue.TryDequeue(out _);
        }
        Queue.Enqueue(item);
    }

    public T? Find(Func<T, bool> predicate)
    {
        return Queue.FirstOrDefault(predicate);
    }

}