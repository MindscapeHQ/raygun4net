using System;
using System.Runtime.InteropServices;
using System.Linq;
using MonoTouch.Foundation;
using System.Collections.Generic;
using System.IO;

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

    private const string CrashReportDirectory = "testing";

    public static void EnableCrashReporting(string apiKey, Action applicationStart)
    {
      IntPtr sigbus = Marshal.AllocHGlobal (512);
      IntPtr sigsegv = Marshal.AllocHGlobal (512);

      // Store Mono SIGSEGV and SIGBUS handlers
      sigaction (Signal.SIGBUS, IntPtr.Zero, sigbus);
      sigaction (Signal.SIGSEGV, IntPtr.Zero, sigsegv);

      Mindscape.Raygun4Net.Xamarin.iOS.Native.Raygun.SharedReporterWithApiKey (apiKey);

      // Restore Mono SIGSEGV and SIGBUS handlers
      sigaction (Signal.SIGBUS, sigbus, IntPtr.Zero);
      sigaction (Signal.SIGSEGV, sigsegv, IntPtr.Zero);

      PopulateCrashReportDirectoryStructure ();

      try
      {
        applicationStart();
      }
      catch (Exception exception)
      {
        WriteExceptionInformation (exception);
        throw;
      }
    }

    private static void PopulateCrashReportDirectoryStructure()
    {
      var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
      var cache = Path.Combine (documents, "..", "Library", "Caches");
      Directory.CreateDirectory (Path.Combine (cache, CrashReportDirectory));
    }

    private static void WriteExceptionInformation(Exception exception)
    {
      var path = Path.Combine (CrashReportDirectory, string.Format ("%.0f", NSDate.Now.SecondsSinceReferenceDate));

      Console.WriteLine ("Writing exception information to : {0}", path);

      File.WriteAllText (path, exception.StackTrace);
    }
  }
}

