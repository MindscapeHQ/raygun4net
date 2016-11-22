using System;
using System.Collections.Generic;
using System.Linq;

#if __UNIFIED__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

using Mindscape.Raygun4Net;

namespace Raygun.iOS.Sample
{
  public class Application
  {
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
      RaygunClient.Initialize("YOUR_APP_API_KEY").AttachCrashReporting().AttachPulse();
      // if you want to use a different Application Delegate class from "AppDelegate"
      // you can specify it here.
      UIApplication.Main(args, null, "AppDelegate");
    }
  }
}