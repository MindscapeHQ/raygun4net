using System;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Platforms;

namespace Mindscape.Raygun4Net
{
  public static class UnhandledExceptionBridge
  {
    internal delegate void UnhandledExceptionHandler(Exception exception, bool isTerminating);
    
    // We'll route all unhandled exceptions through this one event, from there the RaygunClient
    // can determine whether to send them or not
    private static event UnhandledExceptionHandler UnhandledExceptionEvent;

    static UnhandledExceptionBridge()
    {
      // Attach always available exception handlers
      AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
      {
        RaiseUnhandledException(args.ExceptionObject as Exception, args.IsTerminating);
      };

      TaskScheduler.UnobservedTaskException += (sender, args) =>
      {
        RaiseUnhandledException(args.Exception, false);
      };
      
      // Try attach platform specific exceptions
      WindowsPlatform.TryAttachExceptionHandlers();
      AndroidPlatform.TryAttachExceptionHandlers();
      ApplePlatform.TryAttachExceptionHandlers();

    }
    
    public static void RaiseUnhandledException(Exception exception, bool isTerminating)
    {
      UnhandledExceptionEvent?.Invoke(exception, isTerminating);
    }

    internal static void OnUnhandledException(UnhandledExceptionHandler callback)
    {
      // Wrap the callback in a weak reference container
      // Then subscribe to the event using the weak reference
      // The onDestroyed function is called - and unregisters it from the multicast delegate
      var weakHandler = new WeakExceptionHandler(callback, w => UnhandledExceptionEvent -= w.Handle);
      UnhandledExceptionEvent += weakHandler.Handle;
    }

    private class WeakExceptionHandler
    {
      private readonly Action<WeakExceptionHandler> _onReferenceDestroyed;
      private readonly WeakReference<UnhandledExceptionHandler> _reference;

      public WeakExceptionHandler(UnhandledExceptionHandler handler, Action<WeakExceptionHandler> onReferenceDestroyed)
      {
        _onReferenceDestroyed = onReferenceDestroyed;
        _reference = new WeakReference<UnhandledExceptionHandler>(handler);
      }

      public void Handle(Exception exception, bool isTerminating)
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