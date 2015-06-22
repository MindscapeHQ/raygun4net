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
  internal interface Raygun {

    [Static, Export ("sharedReporterWithApiKey:")]
    Raygun SharedReporterWithApiKey (string apiKey);

    [Export ("identify:")]
    void Identify ([NullAllowed] string userId);

    [Export ("identifyWithUserInfo:")]
    void IdentifyWithUserInfo ([NullAllowed] RaygunUserInfo userInfo);

    [Export ("crash")]
    void Crash();

    [Export ("nextReportUUID")]
    string NextReportUUID { get; }
  }

  [BaseType (typeof (NSObject))]
  internal interface RaygunUserInfo {
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
