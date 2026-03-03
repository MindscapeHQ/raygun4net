using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mindscape.Raygun4Net.Platforms
{
  internal static class AndroidPlatform
  {
    private static Assembly AndroidAssembly;

#if NET6_0_OR_GREATER
    [DynamicDependency("UnhandledExceptionRaiser", "Android.Runtime.AndroidEnvironment", "Mono.Android")]
    [DynamicDependency("add_UnhandledExceptionRaiser", "Android.Runtime.AndroidEnvironment", "Mono.Android")]
    [DynamicDependency("Exception", "Android.Runtime.RaiseThrowableEventArgs", "Mono.Android")]
    [DynamicDependency("get_Exception", "Android.Runtime.RaiseThrowableEventArgs", "Mono.Android")]
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
        AndroidAssembly ??= AssemblyHelpers.FindAssembly("Mono.Android", AssemblyHelpers.HexStringToByteArray("84e04ff9cfb79065"));
        
        if (AndroidAssembly is null)
        {
          return false;
        }

        // Reflection equivalent of: 
        // Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += Handler
        var androidRuntimeType = AndroidAssembly.GetType("Android.Runtime.AndroidEnvironment");
        var eventInfo = androidRuntimeType?.GetEvent("UnhandledExceptionRaiser");

        if (androidRuntimeType is null || eventInfo?.EventHandlerType is null)
        {
          Debug.WriteLine("Could not resolve Android.Runtime types - they may have been trimmed.");
          return false;
        }

        // We need to create a wrapper around the target because the handler is fired with
        // RaiseThrowableEventArgs, which is a type we don't have access to
        var eventHandler = new EventHandler(AndroidUnhandledExceptionHandler);
        var typedHandler =
          Delegate.CreateDelegate(eventInfo.EventHandlerType, eventHandler.Target, eventHandler.Method);

        // static event handler
        eventInfo.AddEventHandler(null, typedHandler);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error attaching to AndroidEnvironment.UnhandledExceptionRaiser: {0}", ex);
        return false;
      }

      return true;
    }

#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2075",
      Justification = "Platform types are resolved via reflection from conditionally loaded assemblies.")]
#endif
    private static void AndroidUnhandledExceptionHandler(object sender, object exceptionArgs)
    {
      Exception exception;
      try
      {
        var exceptionArgsType = exceptionArgs.GetType();
        exception = (Exception)exceptionArgsType.GetProperty("Exception").GetValue(exceptionArgs);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Failure extracting exception details in AndroidEnvironment.UnhandledExceptionRaiser: {0}", ex);
        return;
      }
      
      UnhandledExceptionBridge.RaiseUnhandledException(exception, false);
    }
  }
}