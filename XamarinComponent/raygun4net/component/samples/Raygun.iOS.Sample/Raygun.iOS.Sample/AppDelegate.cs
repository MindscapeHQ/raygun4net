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

namespace Raygun.iOS.Sample
{
  [Register("AppDelegate")]
  public partial class AppDelegate : UIApplicationDelegate
  {
    UIWindow window;
    MyViewController viewController;

    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
      window = new UIWindow(UIScreen.MainScreen.Bounds);

      viewController = new MyViewController();
      window.RootViewController = viewController;

      window.MakeKeyAndVisible();

      return true;
    }
  }
}

