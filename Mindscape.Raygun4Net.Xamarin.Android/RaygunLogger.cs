using Android.Util;

namespace Mindscape.Raygun4Net
{
  public class RaygunLogger
  {
    private static string TAG = "Raygun";

    public static void Debug(string message)
    {
      Log.Debug(TAG, message);
    }

    public static void Info(string message)
    {
      Log.Info(TAG, message);
    }

    public static void Warning(string message)
    {
      Log.Warn(TAG, message);
    }

    public static void Error(string message)
    {
      Log.Error(TAG, message);
    }

    public static void Verbose(string message)
    {
      Log.Verbose(TAG, message);
    }

    public static void LogResponseStatusCode(int statusCode)
    {
      switch (statusCode)
      {
        case (int)RaygunResponseStatusCode.RaygunResponseStatusCodeAccepted:
          Debug(RaygunResponseStatusCodeConverter.ToString(statusCode));
          break;

        case (int)RaygunResponseStatusCode.RaygunResponseStatusCodeBadMessage:    // Fall through
        case (int)RaygunResponseStatusCode.RaygunResponseStatusCodeInvalidApiKey: // Fall through
        case (int)RaygunResponseStatusCode.RaygunResponseStatusCodeLargePayload:  // Fall through
        case (int)RaygunResponseStatusCode.RaygunResponseStatusCodeRateLimited:   // Fall through
          Error(RaygunResponseStatusCodeConverter.ToString(statusCode));
          break;

        default: Debug(RaygunResponseStatusCodeConverter.ToString(statusCode)); break;
      }
    }
  }
}
