using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Mindscape.Raygun4Net;

namespace Raygun.Android.Sample
{
  [Activity(Label = "Raygun.Android.Sample", MainLauncher = true, Icon = "@drawable/icon")]
  public class MainActivity : Activity
  {
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      RaygunClient.Initialize("YOUR_APP_API_KEY").AttachCrashReporting().AttachPulse(this);

      // Set our view from the "main" layout resource
      SetContentView(Resource.Layout.Main);

      // Get our button from the layout resource,
      // and attach an event to it
      Button button = FindViewById<Button>(Resource.Id.MyButton);

      button.Click += delegate
      {
        throw new Exception("Something has gone horribly wrong");
      };
    }
  }
}

