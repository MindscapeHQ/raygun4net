using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mindscape.Raygun4Net.Platforms
{
  internal static class WindowsPlatform
  {
    private static Assembly WinUIAssembly;

    private static Exception _lastFirstChanceException;

#if NET6_0_OR_GREATER
    [DynamicDependency("UnhandledException", "Microsoft.UI.Xaml.Application", "Microsoft.WinUI")]
#endif
    public static bool TryAttachExceptionHandlers()
    {
      try
      {
        WinUIAssembly ??= AssemblyHelpers.FindAssembly("Microsoft.WinUI", AssemblyHelpers.HexStringToByteArray("de31ebe4ad15742b"));
        
        if (WinUIAssembly is null)
        {
          return false;
        }

        // Reflection equivalent of: 
        // Microsoft.UI.Xaml.Application.Current.UnhandledException += Current_UnhandledException;

        // Get the Type of Microsoft.UI.Xaml.Application
        var applicationType = WinUIAssembly.GetType("Microsoft.UI.Xaml.Application");
        var eventInfo = applicationType.GetEvent("UnhandledException");
        var application = applicationType.GetProperty("Current").GetValue(null);

        // We need to create a wrapper around the target because the handler is fired with
        // Microsoft.UI.Xaml.UnhandledExceptionEventArgs rather than System.UnhandledExceptionEventArgs
        var eventHandler = new EventHandler(WinUIUnhandledExceptionHandler);
        var typedHandler =
          Delegate.CreateDelegate(eventInfo.EventHandlerType!, eventHandler.Target, eventHandler.Method);

        eventInfo.AddEventHandler(application, typedHandler);
        
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
          _lastFirstChanceException = args.Exception;
        };
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error attaching to Microsoft.UI.Xaml.Application.Current.UnhandledException: {0}", ex);
        return false;
      }

      return true;
    }

    private static void WinUIUnhandledExceptionHandler(object sender, object exceptionArgs)
    {
      bool handled;
      Exception exception;
      try
      {
        // Disgusting because the underlying type is actually different than the generally available type
        // Microsoft.UI.Xaml.UnhandledExceptionEventArgs vs System.UnhandledExceptionEventArgs
        var exceptionType = exceptionArgs.GetType();
        handled = (bool)exceptionType.GetProperty("Handled").GetValue(exceptionArgs);
        exception = (Exception)exceptionType.GetProperty("Exception").GetValue(exceptionArgs);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Failure extracting exception details in Application.Current.UnhandledException: {0}", ex);
        return;
      }

      if (exception?.StackTrace is null)
      {
        exception = _lastFirstChanceException;
      }

      UnhandledExceptionBridge.RaiseUnhandledException(exception, !handled);
    }
  }
}