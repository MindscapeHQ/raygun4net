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
    internal delegate void UnhandledExceptionHandlerWithTag(Exception exception, bool isTerminating, string tag);

    private static readonly List<WeakExceptionHandler> Handlers = new List<WeakExceptionHandler>();

    private static readonly ReaderWriterLockSlim HandlersLock = new ReaderWriterLockSlim();

    static UnhandledExceptionBridge()
    {
      // Attach always available exception handlers
      AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
      {
        RaiseUnhandledException(args.ExceptionObject as Exception, args.IsTerminating);
      };

      TaskScheduler.UnobservedTaskException += (sender, args) => { RaiseUnhandledException(args.Exception,false, "UnobservedTaskException"); };

      // Try attach platform specific exceptions
      WindowsPlatform.TryAttachExceptionHandlers();
      AndroidPlatform.TryAttachExceptionHandlers();
      ApplePlatform.TryAttachExceptionHandlers();
    }

    public static void RaiseUnhandledException(Exception exception, bool isTerminating, string tag = "UnhandledException")
    {
      HandlersLock.EnterReadLock();
      try
      {
        foreach (var handler in Handlers)
        {
          handler.Invoke(exception, isTerminating, tag);
        }
      }
      finally
      {
        HandlersLock.ExitReadLock();
      }
    }

    internal static void OnUnhandledException(UnhandledExceptionHandler callback)
    {
      OnUnhandledException((exception, isTerminating, tag) => callback(exception, isTerminating));
    }
    
    internal static void OnUnhandledException(UnhandledExceptionHandlerWithTag callback)
    {
      var weakHandler = new WeakExceptionHandler(callback);

      HandlersLock.EnterWriteLock();
      try
      {
        Handlers.Add(weakHandler);
        
        // Remove any handlers where their references are no longer alive
        var handlersToRemove = Handlers.Where(x => !x.IsAlive).ToList();
        foreach (var handler in handlersToRemove)
        {
          Handlers.Remove(handler);
        }
      }
      finally
      {
        HandlersLock.ExitWriteLock();
      }
    }

    private class WeakExceptionHandler
    {
      private readonly WeakReference<UnhandledExceptionHandlerWithTag> _reference;

      public bool IsAlive => _reference.TryGetTarget(out _);

      public WeakExceptionHandler(UnhandledExceptionHandlerWithTag handler)
      {
        _reference = new WeakReference<UnhandledExceptionHandlerWithTag>(handler);
      }

      public void Invoke(Exception exception, bool isTerminating, string tag)
      {
        if (!_reference.TryGetTarget(out var handle))
        {
          return;
        }
        
        handle.Invoke(exception, isTerminating, tag);
      }
    }
  }
}