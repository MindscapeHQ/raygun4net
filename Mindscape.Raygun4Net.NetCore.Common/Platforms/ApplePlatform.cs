using System;
using System.Diagnostics;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Mindscape.Raygun4Net.Platforms
{
  internal static class ApplePlatform
  {
    private static Assembly IOSAssembly;

    private static Assembly MacCatalystAssembly;

    private static object MarshalManagedExceptionMode_UnwindNativeCode;

#if NET6_0_OR_GREATER
    // Preserves the MarshalManagedException method and MarshalManagedExceptionMode enum
    // from being trimmed by the compiler.
    // Not supported in netstandard2.0.
    [DynamicDependency("MarshalManagedException", "ObjCRuntime.Runtime", "Microsoft.iOS")]
    [DynamicDependency("MarshalManagedExceptionMode", "ObjCRuntime", "Microsoft.iOS")]
    [DynamicDependency("MarshalManagedException", "ObjCRuntime.Runtime", "Microsoft.MacCatalyst")]
    [DynamicDependency("MarshalManagedExceptionMode", "ObjCRuntime", "Microsoft.MacCatalyst")]
    [UnconditionalSuppressMessage("Trimming", "IL2035",
      Justification = "Platform assemblies are conditionally loaded at runtime; missing assemblies are expected on non-target platforms.")]
#endif
    public static bool TryAttachExceptionHandlers()
    {
      try
      {
        IOSAssembly ??= AssemblyHelpers.FindAssembly("Microsoft.iOS", AssemblyHelpers.HexStringToByteArray("84e04ff9cfb79065"));

        MacCatalystAssembly ??= AssemblyHelpers.FindAssembly("Microsoft.MacCatalyst", AssemblyHelpers.HexStringToByteArray("84e04ff9cfb79065"));

        // One or the other, the types and names are the same across both
        var activeAssembly = IOSAssembly ?? MacCatalystAssembly;

        if (activeAssembly is null)
        {
          return false;
        }

        /*
         * Reflection equivalent of
         *
         * ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
         * {
         *   args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
         * }
         */

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
        Debug.WriteLine("Error attaching to ObjCRuntime.Runtime.MarshalManagedException: {0}", ex);
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
      catch (Exception ex)
      {
        Debug.WriteLine("Could not set ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode: {0}", ex);
      }
    }
  }
}