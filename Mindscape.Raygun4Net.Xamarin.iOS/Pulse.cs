using System;
using System.Runtime.InteropServices;
using MonoTouch;
using System.Diagnostics;
using Mindscape.Raygun4Net.Messages;
using System.Collections.Generic;

#if __UNIFIED__
using UIKit;
using ObjCRuntime;
using Foundation;
#else
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;
using MonoTouch.Foundation;
#endif

namespace Mindscape.Raygun4Net
{
  internal static class Pulse
  {
    private static RaygunClient _raygunClient;
    private static readonly Dictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();
    private static string _lastViewName;

    private static NSObject _didBecomeActiveObserver;
    private static NSObject _didEnterBackgroundObserver;
    private static NSObject _willResignActiveObserver;

    internal static void Attach(RaygunClient raygunClient)
    {
      if (_raygunClient == null && raygunClient != null)
      {
        _raygunClient = raygunClient;

        AttachNotifications();

        Hijack(new UIViewController(), "loadView", ref original_loadView_impl, LoadViewCapture);
        Hijack(new UIViewController(), "viewDidLoad", ref original_viewDidLoad_impl, ViewDidLoadCapture);
        Hijack(new UIViewController(), "viewWillAppear:", ref original_viewWillAppear_impl, ViewWillAppearCapture);
        Hijack(new UIViewController(), "viewDidAppear:", ref original_viewDidAppear_impl, ViewDidAppearCapture);
      }
    }

    private static void AttachNotifications()
    {
      _didBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, OnDidBecomeActive);
      _didEnterBackgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnDidEnterBackground);
      _willResignActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, OnWillResignActive);
    }

    internal static void Detach()
    {
      if (_raygunClient != null) 
      {
        DetachNotifications();

        Restore(new UIViewController(), "loadView", original_loadView_impl);
        Restore(new UIViewController(), "viewDidLoad", original_viewDidLoad_impl);
        Restore(new UIViewController(), "viewWillAppear:", original_viewWillAppear_impl);
        Restore(new UIViewController(), "viewDidAppear:", original_viewDidAppear_impl);

        _raygunClient = null;
      }
    }

    private static void DetachNotifications()
    {
      if (_didBecomeActiveObserver != null)
      {
        _didBecomeActiveObserver.Dispose();
      }

      if (_didEnterBackgroundObserver != null)
      {
        _didEnterBackgroundObserver.Dispose();
      }

      if (_willResignActiveObserver != null)
      {
        _willResignActiveObserver.Dispose();
      }
    }

    private static void OnDidBecomeActive(NSNotification notification)
    {
      _raygunClient.EnsurePulseSessionStarted();

      if (_lastViewName != null) 
      {
        _raygunClient.SendPulseTimingEvent(RaygunPulseEventType.ViewLoaded, _lastViewName, 0);
      }
    }

    private static void OnDidEnterBackground(NSNotification notification)
    {
      _raygunClient.EnsurePulseSessionEnded();
    }

    private static void OnWillResignActive(NSNotification notification)
    {
      _raygunClient.EnsurePulseSessionEnded();
    }

    internal static void SendRemainingViews() 
    {
      if (_raygunClient != null) 
      {
        foreach(string view in _timers.Keys) 
        {
          long duration = 0;
          Stopwatch stopwatch;
          _timers.TryGetValue(view, out stopwatch);

          if (stopwatch != null) 
          {
            stopwatch.Stop();
            duration = stopwatch.ElapsedMilliseconds;
          }
          _raygunClient.SendPulseTimingEventNow(RaygunPulseEventType.ViewLoaded, view, duration);
        }

        _raygunClient.EnsurePulseSessionEnded();
      }
    }

    // Swizzling

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static IntPtr class_getInstanceMethod(IntPtr classHandle, IntPtr Selector);

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static IntPtr imp_implementationWithBlock(ref BlockLiteral block);

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static void method_setImplementation(IntPtr method, IntPtr imp);

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static IntPtr method_getImplementation(IntPtr method);

    delegate void CaptureDelegate(IntPtr block, IntPtr self);

    delegate void CaptureBooleanDelegate(IntPtr block, IntPtr self, bool b);

    [MonoNativeFunctionWrapper]
    public delegate void OriginalDelegate(IntPtr self);

    [MonoNativeFunctionWrapper]
    public delegate void OriginalBooleanDelegate(IntPtr self, bool b);

    private static void Hijack(NSObject obj, string selector, ref IntPtr originalImpl, CaptureDelegate captureDelegate)
    {
      var method = class_getInstanceMethod (obj.ClassHandle, new Selector (selector).Handle);
      originalImpl = method_getImplementation (method);
      if (originalImpl != IntPtr.Zero) 
      {
        var block_value = new BlockLiteral();
        block_value.SetupBlock(captureDelegate, null);
        var imp = imp_implementationWithBlock(ref block_value);
        method_setImplementation(method, imp);
      }
    }

    private static void Hijack(NSObject obj, string selector, ref IntPtr originalImpl, CaptureBooleanDelegate captureDelegate)
    {
      var method = class_getInstanceMethod (obj.ClassHandle, new Selector (selector).Handle);
      originalImpl = method_getImplementation (method);
      if (originalImpl != IntPtr.Zero)
      {
        var block_value = new BlockLiteral();
        block_value.SetupBlock(captureDelegate, null);
        var imp = imp_implementationWithBlock(ref block_value);
        method_setImplementation(method, imp);
      }
    }

    private static void Restore(NSObject obj, string selector, IntPtr originalImpl)
    {
      if (originalImpl != IntPtr.Zero) 
      {
        var method = class_getInstanceMethod(obj.ClassHandle, new Selector(selector).Handle);
        method_setImplementation(method, originalImpl);
      }
    }

    // loadView

    private static IntPtr original_loadView_impl;

    [MonoPInvokeCallback (typeof (CaptureDelegate))]
    static void LoadViewCapture(IntPtr block, IntPtr self)
    {
      NSObject obj = Runtime.GetNSObject(self);
      string pageName = GetPageName(obj.ToString());

      if (IsValidPageName(pageName)) 
      {
        Stopwatch stopwatch;
        _timers.TryGetValue(pageName, out stopwatch);
        if (stopwatch == null) 
        {
          stopwatch = new Stopwatch();
          _timers[pageName] = stopwatch;
          stopwatch.Start();
        }
      }

      var orig = (OriginalDelegate) Marshal.GetDelegateForFunctionPointer(original_loadView_impl, typeof(OriginalDelegate));
      orig(self);
    }

    // viewDidLoad

    private static IntPtr original_viewDidLoad_impl;

    [MonoPInvokeCallback (typeof (CaptureDelegate))]
    static void ViewDidLoadCapture(IntPtr block, IntPtr self)
    {
      NSObject obj = Runtime.GetNSObject(self);
      string pageName = GetPageName(obj.ToString());

      if (IsValidPageName(pageName)) 
      {
        Stopwatch stopwatch;
        _timers.TryGetValue(pageName, out stopwatch);
        if (stopwatch == null) 
        {
          stopwatch = new Stopwatch();
          _timers[pageName] = stopwatch;
          stopwatch.Start();
        }
      }

      var orig = (OriginalDelegate) Marshal.GetDelegateForFunctionPointer(original_viewDidLoad_impl, typeof(OriginalDelegate));
      orig(self);
    }

    // viewWillAppear

    private static IntPtr original_viewWillAppear_impl;

    [MonoPInvokeCallback (typeof (CaptureBooleanDelegate))]
    static void ViewWillAppearCapture(IntPtr block, IntPtr self, bool animated)
    {
      NSObject obj = Runtime.GetNSObject(self);
      string pageName = GetPageName(obj.ToString());

      if (IsValidPageName(pageName)) 
      {
        Stopwatch stopwatch;
        _timers.TryGetValue(pageName, out stopwatch);
        if (stopwatch == null) 
        {
          stopwatch = new Stopwatch();
          _timers[pageName] = stopwatch;
          stopwatch.Start();
        }
      }

      var orig = (OriginalBooleanDelegate) Marshal.GetDelegateForFunctionPointer(original_viewWillAppear_impl, typeof(OriginalBooleanDelegate));
      orig(self, animated);
    }

    // viewDidAppear

    private static IntPtr original_viewDidAppear_impl;

    [MonoPInvokeCallback (typeof (CaptureBooleanDelegate))]
    static void ViewDidAppearCapture(IntPtr block, IntPtr self, bool animated)
    {
      var orig = (OriginalBooleanDelegate) Marshal.GetDelegateForFunctionPointer(original_viewDidAppear_impl, typeof(OriginalBooleanDelegate));
      orig(self, animated);

      NSObject obj = Runtime.GetNSObject(self);
      string pageName = GetPageName(obj.ToString());
      _lastViewName = pageName;

      Stopwatch stopwatch;
      _timers.TryGetValue(pageName, out stopwatch);
      long duration = 0;

      if (stopwatch != null) 
      {
        stopwatch.Stop();
        duration = stopwatch.ElapsedMilliseconds;
        _timers.Remove(pageName);
      }

      if (IsValidPageName(pageName)) 
      {
        _raygunClient.SendPulseTimingEvent(RaygunPulseEventType.ViewLoaded, pageName, duration);
      }
    }

    // Helpers

    private static string GetPageName(string objectName)
    {
      string pageName = objectName.Replace("<", string.Empty).Replace(">", string.Empty);
      int index = pageName.IndexOf('_');

      if (index >= 0 && index < pageName.Length) 
      {
        pageName = pageName.Substring(index + 1);
      }

      index = pageName.IndexOf(':');

      if (index >= 0 && index < pageName.Length) 
      {
        pageName = pageName.Substring(0, index);
      }
      return pageName;
    }

    private static bool IsValidPageName(string pageName) 
    {
      if (!"UINavigationController".Equals(pageName) && 
          !"UIInputWindowController".Equals(pageName) && 
          !"UIStatusBarViewController".Equals(pageName)) 
      {
        return true;
      }
      return false;
    }
  }
}

