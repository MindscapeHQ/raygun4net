﻿using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Mindscape.Raygun4Net;

namespace Raygun.iOS.Sample
{
  public class Application
  {
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
      RaygunClient.Attach ("YOUR_APP_API_KEY");
      // if you want to use a different Application Delegate class from "AppDelegate"
      // you can specify it here.
      UIApplication.Main(args, null, "AppDelegate");
    }
  }
}