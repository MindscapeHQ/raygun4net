using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Diagnostics;

namespace Mindscape.Raygun4Net
{
  internal class Pulse : Java.Lang.Object, Android.App.Application.IActivityLifecycleCallbacks
  {
    private static RaygunClient _raygunClient;
    private static Pulse _pulse;
    private static Activity _mainActivity;

    private static Activity _currentActivity;
    private static readonly Stopwatch _timer = new Stopwatch();

    internal static void Attach(RaygunClient raygunClient, Activity mainActivity)
    {
      if (_pulse == null && raygunClient != null && mainActivity != null && mainActivity.Application != null)
      {
        _raygunClient = raygunClient;
        _mainActivity = mainActivity;
        _pulse = new Pulse();
        _mainActivity.Application.RegisterActivityLifecycleCallbacks(_pulse);

        _raygunClient.SendPulseSessionEvent(RaygunPulseSessionEventType.SessionStart);
        _currentActivity = _mainActivity;
        _timer.Start();
      }
    }

    internal static void Detach()
    {
      if (_pulse != null && _mainActivity != null && _mainActivity.Application != null)
      {
        _mainActivity.Application.UnregisterActivityLifecycleCallbacks(_pulse);
        _mainActivity = null;
        _currentActivity = null;
        _pulse = null;
        _raygunClient = null;
      }
    }

    internal static void SendRemainingActivity()
    {
      if (_pulse != null)
      {
        if (_timer.IsRunning && _currentActivity != null)
        {
          _timer.Stop();
          string activityName = GetActivityName(_currentActivity);
          _raygunClient.SendPulseTimingEventNow(RaygunPulseEventType.ActivityLoaded, activityName, _timer.ElapsedMilliseconds);
        }
        _raygunClient.SendPulseSessionEventNow(RaygunPulseSessionEventType.SessionEnd);
      }
    }

    public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
    {
      if (_currentActivity == null)
      {
        _raygunClient.SendPulseSessionEvent(RaygunPulseSessionEventType.SessionStart);
      }

      if (activity != _currentActivity)
      {
        _currentActivity = activity;
        _timer.Restart();
      }
      //Console.WriteLine("ACTIVITY CREATED " + activity.Title);
    }

    public void OnActivityStarted(Activity activity)
    {
      if (_currentActivity == null)
      {
        _raygunClient.SendPulseSessionEvent(RaygunPulseSessionEventType.SessionStart);
      }

      if (activity != _currentActivity)
      {
        _currentActivity = activity;
        _timer.Restart();
      }
      //Console.WriteLine("ACTIVITY STARTED " + activity.Title);
    }

    public void OnActivityResumed(Activity activity)
    {
      if (_currentActivity == null)
      {
        _raygunClient.SendPulseSessionEvent(RaygunPulseSessionEventType.SessionStart);
      }

      string activityName = GetActivityName(activity);
      decimal duration = 0;
      if (activity == _currentActivity)
      {
        _timer.Stop();
        duration = _timer.ElapsedMilliseconds;
      }
      _currentActivity = activity;

      _raygunClient.SendPulseTimingEvent(RaygunPulseEventType.ActivityLoaded, activityName, duration);
      //Console.WriteLine("ACTIVITY RESUMED " + activity.Title + " DURATION: " + duration);
    }

    public void OnActivityPaused(Activity activity)
    {
      //Console.WriteLine("ACTIVITY PAUSED " + activity.Title);
    }

    public void OnActivityStopped(Activity activity)
    {
      if (activity == _currentActivity)
      {
        _currentActivity = null;
        _raygunClient.SendPulseSessionEvent(RaygunPulseSessionEventType.SessionEnd);
      }
      //Console.WriteLine("ACTIVITY STOPPED " + activity.Title);
    }

    public void OnActivityDestroyed(Activity activity)
    {
      //Console.WriteLine("ACTIVITY DESTROYED " + activity.Title);
    }

    public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
    {

    }

    private static string GetActivityName(Activity activity)
    {
      return activity.GetType().Name;
    }
  }
}