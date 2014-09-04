using System;
using System.Runtime.InteropServices;
using System.Linq;
using MonoTouch.Foundation;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

    private const string StackTraceDirectory = "stacktraces";

    public static void EnableCrashReporting(string apikey)
    {
      PopulateCrashReportDirectoryStructure ();

      IntPtr sigbus = Marshal.AllocHGlobal (512);
      IntPtr sigsegv = Marshal.AllocHGlobal (512);

      // Store Mono SIGSEGV and SIGBUS handlers
      sigaction (Signal.SIGBUS, IntPtr.Zero, sigbus);
      sigaction (Signal.SIGSEGV, IntPtr.Zero, sigsegv);

      var reporter = Mindscape.Raygun4Net.Xamarin.iOS.Native.Raygun.SharedReporterWithApiKey (apikey);

      // Restore Mono SIGSEGV and SIGBUS handlers
      sigaction (Signal.SIGBUS, sigbus, IntPtr.Zero);
      sigaction (Signal.SIGSEGV, sigsegv, IntPtr.Zero);

      Marshal.FreeHGlobal (sigbus);
      Marshal.FreeHGlobal (sigsegv);

      AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
        WriteExceptionInformation (reporter.NextReportUUID, e.ExceptionObject as Exception);
      };
      TaskScheduler.UnobservedTaskException += (sender, e) => {
        WriteExceptionInformation (reporter.NextReportUUID, e.Exception);
      };
    }

    private static void PopulateCrashReportDirectoryStructure()
    {
      var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
      var path = Path.Combine (documents, "..", "Library", "Caches", StackTraceDirectory);
      Directory.CreateDirectory (path);
    }

    private static void WriteExceptionInformation(string identifier, Exception exception)
    {
      if (exception == null) return;

      var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
      var path = Path.GetFullPath(Path.Combine (documents, "..", "Library", "Caches", StackTraceDirectory, string.Format ("{0}", identifier)));

      var exceptionType = exception.GetType();

      File.WriteAllText (path, string.Join(Environment.NewLine, exceptionType.FullName, exception.Message, exception.StackTrace));
    }
  }
}

