using System;
using System.Drawing;
#if __UNIFIED__
using UIKit;
#else
using MonoTouch.UIKit;
#endif

namespace Raygun.iOS.Sample
{
  public class MyViewController : UIViewController
  {
    UIButton button;
    float buttonWidth = 200;
    float buttonHeight = 50;

    public MyViewController()
    {
    }

    public override void ViewDidLoad()
    {
      base.ViewDidLoad();

      View.Frame = UIScreen.MainScreen.Bounds;
      View.BackgroundColor = UIColor.White;
      View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

      button = UIButton.FromType(UIButtonType.RoundedRect);

      button.Frame = new RectangleF((float)(View.Frame.Width / 2 - buttonWidth / 2), (float)(View.Frame.Height / 2 - buttonHeight / 2), buttonWidth, buttonHeight);

      button.SetTitle("Crash", UIControlState.Normal);

      button.TouchUpInside += (object sender, EventArgs e) =>
      {
        throw new Exception("Something has gone horribly wrong");
      };

      button.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin |
          UIViewAutoresizing.FlexibleBottomMargin;

      View.AddSubview(button);
    }

  }
}

