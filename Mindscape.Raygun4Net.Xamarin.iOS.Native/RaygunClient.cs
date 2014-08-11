using System;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.Xamarin.iOS.Native
{
  public static class RaygunClient
  {
    [DllImport ("libc")]
    private static extern int sigaction (Signal sig, IntPtr act, IntPtr oact);

    enum Signal {
      SIGBUS = 10,
      SIGSEGV = 11
    }

    public static void EnableCrashReporting(string apiKey)  {

      IntPtr sigbus = Marshal.AllocHGlobal (512);
      IntPtr sigsegv = Marshal.AllocHGlobal (512);

      // Store Mono SIGSEGV and SIGBUS handlers
      sigaction (Signal.SIGBUS, IntPtr.Zero, sigbus);
      sigaction (Signal.SIGSEGV, IntPtr.Zero, sigsegv);

      Mindscape.Raygun4Net.Xamarin.iOS.Native.Raygun.SharedReporterWithApiKey (apiKey);

      // Restore Mono SIGSEGV and SIGBUS handlers
      sigaction (Signal.SIGBUS, sigbus, IntPtr.Zero);
      sigaction (Signal.SIGSEGV, sigsegv, IntPtr.Zero);
    }
  }
}

