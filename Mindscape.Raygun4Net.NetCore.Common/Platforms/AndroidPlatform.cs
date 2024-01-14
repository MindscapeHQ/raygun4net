using System;
using System.Diagnostics;
using System.Reflection;

namespace Mindscape.Raygun4Net.Platforms
{
  internal static class AndroidPlatform
  {
    private static readonly Assembly AndroidAssembly =
      AssemblyHelpers.FindAssembly("Mono.Android", AssemblyHelpers.HexStringToByteArray("84e04ff9cfb79065"));

    public static bool TryAttachExceptionHandlers()
    {
      try
      {
        if (AndroidAssembly is null)
        {
          return false;
        }

        // Reflection equivalent of: 
        // Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += Handler
        var androidRuntimeType = AndroidAssembly.GetType("Android.Runtime.AndroidEnvironment");
        var eventInfo = androidRuntimeType.GetEvent("UnhandledExceptionRaiser");

        // We need to create a wrapper around the target because the handler is fired with
        // RaiseThrowableEventArgs, which is a type we don't have access to
        var eventHandler = new EventHandler(AndroidUnhandledExceptionHandler);
        var typedHandler =
          Delegate.CreateDelegate(eventInfo.EventHandlerType!, eventHandler.Target, eventHandler.Method);

        // static event handler
        eventInfo.AddEventHandler(null, typedHandler);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error attaching to AndroidEnvironment.UnhandledExceptionRaiser: {0}", ex);
        return false;
      }

      return true;
    }

    private static void AndroidUnhandledExceptionHandler(object sender, object exceptionArgs)
    {
      Exception exception;
      try
      {
        var exceptionArgsType = exceptionArgs.GetType();
        exception = (Exception)exceptionArgsType.GetProperty("Exception").GetValue(exceptionArgs);
        exceptionArgsType.GetProperty("Handled")?.SetValue(exceptionArgs, true);
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