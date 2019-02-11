using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;

namespace Mindscape.Raygun4Net
{
  public class RaygunLogger
  {
    [DllImport(Constants.FoundationLibrary)]
    extern static void NSLog(IntPtr format, IntPtr s);

    static NSString format = new NSString("%@");

    public static void Debug(string message)
    {
      Log(RaygunLogLevel.Debug, message);
    }

    public static void Info(string message)
    {
      Log(RaygunLogLevel.Info, message);
    }

    public static void Warning(string message)
    {
      Log(RaygunLogLevel.Warning, message);
    }

    public static void Error(string message)
    {
      Log(RaygunLogLevel.Error, message);
    }

    public static void Verbose(string message)
    {
      Log(RaygunLogLevel.Verbose, message);
    }

    private static void Log(RaygunLogLevel level, string message)
    {
      try
      {
        using (var ns = new NSString(message))
        {
          NSLog(format.Handle, ns.Handle);
        }
      }
      catch (Exception)
      {
      }
    }

    public static void LogResponseStatusCode(int statusCode)
    {
      switch (statusCode)
      {
        case (int)RaygunResponseStatusCode.Accepted:
          Debug(RaygunResponseStatusCodeConverter.ToString(statusCode));
          break;

        case (int)RaygunResponseStatusCode.BadMessage:    // Fall through
        case (int)RaygunResponseStatusCode.InvalidApiKey: // Fall through
        case (int)RaygunResponseStatusCode.LargePayload:  // Fall through
        case (int)RaygunResponseStatusCode.RateLimited:   // Fall through
          Error(RaygunResponseStatusCodeConverter.ToString(statusCode));
          break;

        default: Debug(RaygunResponseStatusCodeConverter.ToString(statusCode)); break;
      }
    }
  }
}
