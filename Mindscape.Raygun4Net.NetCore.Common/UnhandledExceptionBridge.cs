using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Platforms;

namespace Mindscape.Raygun4Net
{
  public static class UnhandledExceptionBridge
  {
    internal delegate void UnhandledExceptionHandler(Exception exception, bool isTerminating);

    private static readonly List<WeakExceptionHandler> Handlers = new List<WeakExceptionHandler>();

    private static readonly ReaderWriterLockSlim HandlersLock =
      new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    static UnhandledExceptionBridge()
    {
      // Attach always available exception handlers
      AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
      {
        RaiseUnhandledException(args.ExceptionObject as Exception, args.IsTerminating);
      };

      TaskScheduler.UnobservedTaskException += (sender, args) => { RaiseUnhandledException(args.Exception, false); };

      // Try attach platform specific exceptions
      WindowsPlatform.TryAttachExceptionHandlers();
      AndroidPlatform.TryAttachExceptionHandlers();
      ApplePlatform.TryAttachExceptionHandlers();
    }

    public static void RaiseUnhandledException(Exception exception, bool isTerminating)
    {
      HandlersLock.EnterReadLock();
      try
      {
        foreach (var handler in Handlers)
        {
          handler.Invoke(exception, isTerminating);
        }
      }
      finally
      {
        HandlersLock.ExitReadLock();
      }
    }

    internal static void OnUnhandledException(UnhandledExceptionHandler callback)
    {
      // Wrap the callback in a weak reference container
      // Then subscribe to the event using the weak reference
      // The onDestroyed function is called - and unregisters it from the multicast delegate
      var weakHandler = new WeakExceptionHandler(callback, RemoveDeadHandler);

      HandlersLock.EnterWriteLock();
      try
      {
        Handlers.Add(weakHandler);
        RemoveDeadHandlers();
      }
      finally
      {
        HandlersLock.ExitWriteLock();
      }
    }

    private static void RemoveDeadHandler(WeakExceptionHandler handler)
    {
      HandlersLock.EnterWriteLock();
      try
      {
        Handlers.Remove(handler);
      }
      finally
      {
        HandlersLock.ExitWriteLock();
      }
    }

    private static void RemoveDeadHandlers()
    {
      HandlersLock.EnterWriteLock();
      try
      {
        var handlersToRemove = Handlers.Where(x => !x.IsAlive).ToList();
        foreach (var handler in handlersToRemove)
        {
          RemoveDeadHandler(handler);
        }
      }
      finally
      {
        HandlersLock.ExitWriteLock();
      }
    }

    private class WeakExceptionHandler
    {
      private readonly Action<WeakExceptionHandler> _onReferenceDestroyed;
      private readonly WeakReference<UnhandledExceptionHandler> _reference;

      public bool IsAlive => _reference.TryGetTarget(out _);

      public WeakExceptionHandler(UnhandledExceptionHandler handler, Action<WeakExceptionHandler> onReferenceDestroyed)
      {
        _onReferenceDestroyed = onReferenceDestroyed;
        _reference = new WeakReference<UnhandledExceptionHandler>(handler);
      }

      public void Invoke(Exception exception, bool isTerminating)
      {
        // If the target is still alive then forward the invocation
        if (_reference.TryGetTarget(out var handle))
        {
          handle.Invoke(exception, isTerminating);
          return;
        }

        // the reference is dead, so call the clean up function
        _onReferenceDestroyed(this);
      }
    }
  }
}