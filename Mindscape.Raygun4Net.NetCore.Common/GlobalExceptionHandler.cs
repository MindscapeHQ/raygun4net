using System;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net
{
  internal static class GlobalExceptionHandler
  {
    // We'll route all unhandled exceptions through this one event.
    public static event Action<object, UnhandledExceptionEventArgs> UnhandledException;

    static GlobalExceptionHandler()
    {
      AppDomain.CurrentDomain.UnhandledException += (sender, args) => { UnhandledException?.Invoke(sender, args); };

      TaskScheduler.UnobservedTaskException += (sender, args) =>
      {
        UnhandledException?.Invoke(sender, new UnhandledExceptionEventArgs(args.Exception, false));
      };

#if IOS
      // This could also be extended to add support for MacOS and Mac Catalyst
      ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
      {
        args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
      };

#elif ANDROID
      Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
      {
        args.Handled = true;
        UnhandledException?.Invoke(sender, new UnhandledExceptionEventArgs(args.Exception, true));
      };
#endif
    }
  }
}