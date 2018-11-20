using System;
using System.Drawing;

#if __UNIFIED__
using ObjCRuntime;
using Foundation;
using UIKit;
#else
using MonoTouch.ObjCRuntime;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Mindscape.Raygun4Net.Xamarin.iOS
{
  [BaseType (typeof (NSObject))]
  internal interface RaygunClient {

    [Static, Export ("sharedInstanceWithApiKey:")]
    RaygunClient SharedInstanceWithApiKey (string apiKey);

    [Export("enableCrashReporting")]
    void EnableCrashReporting();

    [Export("userInformation", ArgumentSemantic.Strong)]
    [NullAllowed]
    RaygunUserInformation UserInformation { get; set; }

    [Export ("crash")]
    void Crash();
  }

  [BaseType (typeof (NSObject))]
  internal interface RaygunUserInformation {
    [Export ("identifier")]
    [NullAllowed]
    string Identifier { get; set; }

    [Export ("isAnonymous")]
    [NullAllowed]
    bool IsAnonymous { get; set; }

    [Export ("email")]
    [NullAllowed]
    string Email { get; set; }

    [Export ("fullName")]
    [NullAllowed]
    string FullName { get; set; }

    [Export ("firstName")]
    [NullAllowed]
    string FirstName{ get; set; }
  }
}
