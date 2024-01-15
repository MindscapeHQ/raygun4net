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
    internal static event UnhandledExceptionHandler OnUnhandledException;

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
      OnUnhandledException?.Invoke(exception, isTerminating);
    }
  }
}