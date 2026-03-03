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
    [DynamicDependency("add_UnhandledException", "Microsoft.UI.Xaml.Application", "Microsoft.WinUI")]
    [DynamicDependency("Current", "Microsoft.UI.Xaml.Application", "Microsoft.WinUI")]
    [DynamicDependency("get_Current", "Microsoft.UI.Xaml.Application", "Microsoft.WinUI")]
    [DynamicDependency("Handled", "Microsoft.UI.Xaml.UnhandledExceptionEventArgs", "Microsoft.WinUI")]
    [DynamicDependency("get_Handled", "Microsoft.UI.Xaml.UnhandledExceptionEventArgs", "Microsoft.WinUI")]
    [DynamicDependency("Exception", "Microsoft.UI.Xaml.UnhandledExceptionEventArgs", "Microsoft.WinUI")]
    [DynamicDependency("get_Exception", "Microsoft.UI.Xaml.UnhandledExceptionEventArgs", "Microsoft.WinUI")]
    [UnconditionalSuppressMessage("Trimming", "IL2026",
      Justification = "Assembly.GetType() is used to resolve platform types from conditionally loaded assemblies; types are preserved via DynamicDependency.")]
    [UnconditionalSuppressMessage("Trimming", "IL2035",
      Justification = "Platform assemblies are conditionally loaded at runtime; missing assemblies are expected on non-target platforms.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
      Justification = "Platform types are resolved via reflection from conditionally loaded assemblies; members are preserved via DynamicDependency.")]
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
        var eventInfo = applicationType?.GetEvent("UnhandledException");
        var currentProperty = applicationType?.GetProperty("Current");

        if (applicationType is null || eventInfo?.EventHandlerType is null || currentProperty is null)
        {
          Debug.WriteLine("Could not resolve Microsoft.UI.Xaml types - they may have been trimmed.");
          return false;
        }

        var application = currentProperty.GetValue(null);

        // We need to create a wrapper around the target because the handler is fired with
        // Microsoft.UI.Xaml.UnhandledExceptionEventArgs rather than System.UnhandledExceptionEventArgs
        var eventHandler = new EventHandler(WinUIUnhandledExceptionHandler);
        var typedHandler =
          Delegate.CreateDelegate(eventInfo.EventHandlerType, eventHandler.Target, eventHandler.Method);

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

#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2075",
      Justification = "Platform types are resolved via reflection from conditionally loaded assemblies.")]
#endif
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