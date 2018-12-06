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
  }
}
