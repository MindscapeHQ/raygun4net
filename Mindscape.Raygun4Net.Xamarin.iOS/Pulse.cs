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
    private static readonly Dictionary<string, DateTime?> _timers = new Dictionary<string, DateTime?>();
    private static string _lastViewName;

    private static NSObject _didBecomeActiveObserver;
    private static NSObject _didEnterBackgroundObserver;
    private static NSObject _willResignActiveObserver;

    internal static void Attach(RaygunClient raygunClient)
    {
      if(_raygunClient == null && raygunClient != null)
      {
        _raygunClient = raygunClient;

        AttachNotifications();

        Hijack(new UIViewController(), "viewDidLoad", ref original_viewDidLoad_impl, ViewDidLoadCapture);
        Hijack(new UIViewController(), "viewDidAppear:", ref original_viewDidAppear_impl, ViewDidAppearCapture);
        //Hijack(new UIViewController(), "viewDidLayoutSubviews", ref original_viewDidLayoutSubviews_impl, ViewDidLayoutSubviewsCapture);
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
      if(_raygunClient != null) {
        DetachNotifications();

        Restore(new UIViewController(), "viewDidLoad", original_viewDidLoad_impl);
        Restore(new UIViewController(), "viewDidAppear:", original_viewDidAppear_impl);

        _raygunClient = null;
      }
    }

    private static void DetachNotifications()
    {
      if(_didBecomeActiveObserver != null) {
        _didBecomeActiveObserver.Dispose();
      }
      if(_didEnterBackgroundObserver != null) {
        _didEnterBackgroundObserver.Dispose();
      }
      if(_willResignActiveObserver != null) {
        _willResignActiveObserver.Dispose();
      }
    }

    private static void OnDidBecomeActive(NSNotification notification)
    {
      //Console.WriteLine("SESSION START");
      _raygunClient.SendPulseEvent(RaygunPulseEventType.SessionStart);
      if(_lastViewName != null) {
        _raygunClient.SendPulsePageTimingEvent(_lastViewName, 0);
      }
    }

    private static void OnDidEnterBackground(NSNotification notification)
    {
      //Console.WriteLine("SESSION END");
      _raygunClient.SendPulseEvent(RaygunPulseEventType.SessionEnd);
    }

    private static void OnWillResignActive(NSNotification notification)
    {
      //Console.WriteLine("SESSION END");
      _raygunClient.SendPulseEvent(RaygunPulseEventType.SessionEnd);
    }

    // Swizzling

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static IntPtr class_getInstanceMethod (IntPtr classHandle, IntPtr Selector);

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static IntPtr imp_implementationWithBlock (ref BlockLiteral block);

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static void method_setImplementation (IntPtr method, IntPtr imp);

    [DllImport ("/usr/lib/libobjc.dylib")]
    extern static IntPtr method_getImplementation (IntPtr method);

    delegate void CaptureDelegate (IntPtr block, IntPtr self);

    delegate void CaptureBooleanDelegate (IntPtr block, IntPtr self, bool b);

    [MonoNativeFunctionWrapper]
    public delegate void OriginalDelegate(IntPtr self);

    [MonoNativeFunctionWrapper]
    public delegate void OriginalBooleanDelegate(IntPtr self, bool b);

    private static void Hijack(NSObject obj, string selector, ref IntPtr originalImpl, CaptureDelegate captureDelegate)
    {
      var method = class_getInstanceMethod (obj.ClassHandle, new Selector (selector).Handle);
      originalImpl = method_getImplementation (method);
      if(originalImpl != IntPtr.Zero) {
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
      if(originalImpl != IntPtr.Zero) {
        var block_value = new BlockLiteral();
        block_value.SetupBlock(captureDelegate, null);
        var imp = imp_implementationWithBlock(ref block_value);
        method_setImplementation(method, imp);
      }
    }

    private static void Restore(NSObject obj, string selector, IntPtr originalImpl)
    {
      if(originalImpl != IntPtr.Zero) {
        var method = class_getInstanceMethod(obj.ClassHandle, new Selector(selector).Handle);
        method_setImplementation(method, originalImpl);
      }
    }

    // viewDidLoad

    private static IntPtr original_viewDidLoad_impl;

    [MonoPInvokeCallback (typeof (CaptureDelegate))]
    static void ViewDidLoadCapture (IntPtr block, IntPtr self)
    {
      var orig = (OriginalDelegate) Marshal.GetDelegateForFunctionPointer( original_viewDidLoad_impl, typeof(OriginalDelegate));
      orig(self);

      NSObject obj = Runtime.GetNSObject(self);
      string pageName = GetPageName(obj.ToString());
      //Console.WriteLine ("Start load " + obj.ToString());

      _timers[pageName] = DateTime.Now;
    }

    // viewDidAppear

    private static IntPtr original_viewDidAppear_impl;

    [MonoPInvokeCallback (typeof (CaptureBooleanDelegate))]
    static void ViewDidAppearCapture (IntPtr block, IntPtr self, bool animated)
    {
      var orig = (OriginalBooleanDelegate) Marshal.GetDelegateForFunctionPointer(original_viewDidAppear_impl, typeof(OriginalBooleanDelegate));
      orig(self, animated);

      NSObject obj = Runtime.GetNSObject(self);
      string pageName = GetPageName(obj.ToString());
      _lastViewName = pageName;

      DateTime? start;
      _timers.TryGetValue(pageName, out start);
      _timers.Remove(pageName);
      decimal duration = 0;
      if(start != null) {
        duration = (decimal)((DateTime.Now - start.Value).TotalMilliseconds);
      }

      if(!"UINavigationController".Equals(pageName) && !"UIInputWindowController".Equals(pageName)) {
        _raygunClient.SendPulsePageTimingEvent(pageName, duration);
        //Console.WriteLine ("did appear " + obj.ToString() + " " + duration);
      }
    }

    // viewDidLayoutSubviews

    /*private static IntPtr original_viewDidLayoutSubviews_impl;

    [MonoPInvokeCallback (typeof (CaptureDelegate))]
    static void ViewDidLayoutSubviewsCapture (IntPtr block, IntPtr self)
    {
      NSObject obj = Runtime.GetNSObject(self);
      Console.WriteLine ("did layout subviews " + obj.ToString());
      var orig = (OriginalDelegate) Marshal.GetDelegateForFunctionPointer( original_viewDidLayoutSubviews_impl, typeof(OriginalDelegate));
      orig(self);
    }*/

    // Helpers

    private static string GetPageName(string objectName)
    {
      string pageName = objectName.Replace("<", string.Empty).Replace(">", string.Empty);
      int index = pageName.IndexOf('_');
      if(index >= 0 && index < pageName.Length) {
        pageName = pageName.Substring(index + 1);
      }
      index = pageName.IndexOf(':');
      if(index >= 0 && index < pageName.Length) {
        pageName = pageName.Substring(0, index);
      }
      return pageName;
    }

    // Initial experiment code

    /*void HijackWillMoveToSuperView ()
    {
      var method = class_getInstanceMethod (new UIView ().ClassHandle, new Selector ("willMoveToSuperview:").Handle);
      original_impl = method_getImplementation (method);
      var block_value = new BlockLiteral ();
      CaptureDelegate d = MyCapture;
      block_value.SetupBlock (d, null);
      var imp = imp_implementationWithBlock (ref block_value);
      method_setImplementation (method, imp);
    }*/

    /*[MonoPInvokeCallback (typeof (CaptureDelegate))]
    private static void MyCapture (IntPtr block, IntPtr self, IntPtr uiView)
    {
      Console.WriteLine ("Moving to: {0}", Runtime.GetNSObject (uiView));
      original_impl (self, uiView);
      Console.WriteLine ("Added");
    }*/

    /*private static IntPtr original_request_impl;

    private static void HijackRequest ()
    {
      var method = class_getInstanceMethod (new NSUrl ("").ClassHandle, new Selector ("URLWithString:").Handle);
      //var method = class_getInstanceMethod (new NSMutableUrlRequest ().ClassHandle, new Selector ("initWithURL:").Handle);
      original_request_impl = method_getImplementation (method);
      var block_value = new BlockLiteral ();
      ActionCaptureDelegate d = MyRequestCapture;
      block_value.SetupBlock (d, null);
      var imp = imp_implementationWithBlock (ref block_value);
      method_setImplementation (method, imp);
    }

    [MonoPInvokeCallback (typeof (ActionCaptureDelegate))]
    private static void MyRequestCapture (IntPtr block, IntPtr self)
    {
      //NSObject obj = Runtime.GetNSObject(self);
      //Stopwatch stopwatch = new Stopwatch();
      //stopwatch.Start();
      //Console.WriteLine ("Start request " + obj.ToString());
      //var orig = (OriginalDelegate) Marshal.GetDelegateForFunctionPointer( original_request_impl, typeof(OriginalDelegate));
      //orig(self);
      //Console.WriteLine ("Stop request " + stopwatch.ElapsedMilliseconds);
      //stopwatch.Stop();

      Console.WriteLine("HIT");
    }*/
  }
}

