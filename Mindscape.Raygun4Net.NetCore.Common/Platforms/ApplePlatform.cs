using System;
using System.Diagnostics;
using System.Reflection;

namespace Mindscape.Raygun4Net.Platforms
{
  internal static class ApplePlatform
  {
    private static readonly Assembly IOSAssembly =
      AssemblyHelpers.FindAssembly("Microsoft.iOS", AssemblyHelpers.HexStringToByteArray("84e04ff9cfb79065"));

    private static readonly Assembly MacCatalystAssembly =
      AssemblyHelpers.FindAssembly("Microsoft.MacCatalyst", AssemblyHelpers.HexStringToByteArray("84e04ff9cfb79065"));

    private static object MarshalManagedExceptionMode_UnwindNativeCode;

    public static bool TryAttachExceptionHandlers()
    {
      try
      {
        // One or the other, the types and names are the same across both
        var activeAssembly = IOSAssembly ?? MacCatalystAssembly;

        if (activeAssembly is null)
          return false;

        /*
         * Reflection equivalent of
         *
         * ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
         * {
         *   args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
         * }
         */

        // Get the Type of Microsoft.UI.Xaml.Application
        var applicationType = activeAssembly.GetType("ObjCRuntime.Runtime");
        var eventInfo = applicationType.GetEvent("MarshalManagedException");
        var enumType = activeAssembly.GetType("ObjCRuntime.MarshalManagedExceptionMode");
        
        MarshalManagedExceptionMode_UnwindNativeCode = Enum.Parse(enumType, "UnwindNativeCode");

        // We need to create a wrapper around the target because the handler is fired with
        // ObjCRuntime.MarshalManagedExceptionEventArgs
        var eventHandler = new EventHandler(SetAppleUnwindNative);
        var typedHandler =
          Delegate.CreateDelegate(eventInfo.EventHandlerType!, eventHandler.Target, eventHandler.Method);

        eventInfo.AddEventHandler(null, typedHandler);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error attaching to ObjCRuntime.Runtime.MarshalManagedException: {0}", ex);
        return false;
      }

      return true;
    }

    private static void SetAppleUnwindNative(object sender, object e)
    {
      try
      {
        // Assuming 'MarshalManagedExceptionEventArgs' is the type of 'e' and it has a property 'ExceptionMode'
        var argsType = e.GetType();
        var exceptionModeProperty = argsType.GetProperty("ExceptionMode");

        // Assuming 'MarshalManagedExceptionMode' is an enum and 'UnwindNativeCode' is a value within that enum
        exceptionModeProperty.SetValue(e, MarshalManagedExceptionMode_UnwindNativeCode);
      }
      catch(Exception ex)
      {
        Debug.WriteLine("Could not set ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode: {0}", ex);
      }
    }
  }
}