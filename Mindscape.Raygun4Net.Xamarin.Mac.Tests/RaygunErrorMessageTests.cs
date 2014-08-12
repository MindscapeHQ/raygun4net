using NUnit.Framework;
using System;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Xamarin.Mac.Tests
{
  [TestFixture()]
  public class Test
  {
    private FakeRaygunErrorMessage _raygunErrorMessage;

    [SetUp()]
    public void SetUp()
    {
      _raygunErrorMessage = new FakeRaygunErrorMessage ();
    }

    [Test()]
    public void ParseStackTrace()
    {
      string stackTrace = "at Xamarin_Mac_ErrorHarness.MainWindowController.<AwakeFromNib>m__0 (object,System.EventArgs) [0x00006] in /Xamarin_Mac_ErrorHarness/MainWindowController.cs:44\n" +
        "at MonoMac.AppKit.ActionDispatcher.OnActivated (MonoMac.Foundation.NSObject) [0x00015] in /source/xamcore/src/AppKit/ActionDispatcher.cs:49\n" +
        "at MonoMac.AppKit.NSApplication.Main (string[]) [0x00041] in /source/xamcore/src/AppKit/NSApplication.cs:105\n" +
        "at Xamarin_Mac_ErrorHarness.MainClass.Main (string[]) [0x00011] in /Xamarin_Mac_ErrorHarness/Main.cs:17";

      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.ExposeParseStackTrace (stackTrace);

      Assert.AreEqual(4, result.Length);

      // Note that the leading "at" gets stripped.
      // The memory address gets stripped in this scenario, might want to add this back in somewhere.
      // Also, the space between the method name and opening parenthesis is removed.
      Assert.AreEqual ("Xamarin_Mac_ErrorHarness.MainWindowController", result [0].ClassName);
      Assert.AreEqual ("/Xamarin_Mac_ErrorHarness/MainWindowController.cs", result [0].FileName);
      Assert.AreEqual ("<AwakeFromNib>m__0(object,System.EventArgs)", result [0].MethodName);
      Assert.AreEqual (44, result [0].LineNumber);

      Assert.AreEqual ("MonoMac.AppKit.ActionDispatcher", result [1].ClassName);
      Assert.AreEqual ("/source/xamcore/src/AppKit/ActionDispatcher.cs", result [1].FileName);
      Assert.AreEqual ("OnActivated(MonoMac.Foundation.NSObject)", result [1].MethodName);
      Assert.AreEqual (49, result [1].LineNumber);

      Assert.AreEqual ("MonoMac.AppKit.NSApplication", result [2].ClassName);
      Assert.AreEqual ("/source/xamcore/src/AppKit/NSApplication.cs", result [2].FileName);
      Assert.AreEqual ("Main(string[])", result [2].MethodName);
      Assert.AreEqual (105, result [2].LineNumber);

      Assert.AreEqual ("Xamarin_Mac_ErrorHarness.MainClass", result [3].ClassName);
      Assert.AreEqual ("/Xamarin_Mac_ErrorHarness/Main.cs", result [3].FileName);
      Assert.AreEqual ("Main(string[])", result [3].MethodName);
      Assert.AreEqual (17, result [3].LineNumber);
    }

    [Test()]
    public void ParseStackTraceWithNoFileNameOrLineNumber()
    {
      string stackTrace = "at Xamarin_Mac_ErrorHarness.MainWindowController.<AwakeFromNib>m__0 (object,System.EventArgs) <0x00037>\n" +
        "at MonoMac.AppKit.ActionDispatcher.OnActivated (MonoMac.Foundation.NSObject) <0x00021>\n" +
        "at MonoMac.AppKit.NSApplication.Main (string[]) <0x00097>\n" +
        "at Xamarin_Mac_ErrorHarness.MainClass.Main (string[]) <0x00027>";

      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.ExposeParseStackTrace (stackTrace);

      Assert.AreEqual(4, result.Length);

      // Note that the leading "at" gets stripped, and the memory address gets dumped in the FileName property so that it doesn't affect the exception-grouping.
      // Also, the space between the method name and opening parenthesis is removed.
      Assert.AreEqual ("Xamarin_Mac_ErrorHarness.MainWindowController", result [0].ClassName);
      Assert.AreEqual ("<0x00037>", result [0].FileName);
      Assert.AreEqual ("<AwakeFromNib>m__0(object,System.EventArgs)", result [0].MethodName);
      Assert.AreEqual (0, result [0].LineNumber);

      Assert.AreEqual ("MonoMac.AppKit.ActionDispatcher", result [1].ClassName);
      Assert.AreEqual ("<0x00021>", result [1].FileName);
      Assert.AreEqual ("OnActivated(MonoMac.Foundation.NSObject)", result [1].MethodName);
      Assert.AreEqual (0, result [1].LineNumber);

      Assert.AreEqual ("MonoMac.AppKit.NSApplication", result [2].ClassName);
      Assert.AreEqual ("<0x00097>", result [2].FileName);
      Assert.AreEqual ("Main(string[])", result [2].MethodName);
      Assert.AreEqual (0, result [2].LineNumber);

      Assert.AreEqual ("Xamarin_Mac_ErrorHarness.MainClass", result [3].ClassName);
      Assert.AreEqual ("<0x00027>", result [3].FileName);
      Assert.AreEqual ("Main(string[])", result [3].MethodName);
      Assert.AreEqual (0, result [3].LineNumber);
    }

    [Test()]
    public void ParseStackTraceWithWrapperMessages()
    {
      string stackTrace = "at (wrapper dynamic-method) object.[MonoMac.AppKit.ActionDispatcher.Void OnActivated(MonoMac.Foundation.NSObject)] (MonoMac.Foundation.NSObject,MonoMac.ObjCRuntime.Selector,MonoMac.Foundation.NSObject) <0x00033>\n" +
        "at (wrapper native-to-managed) object.[MonoMac.AppKit.ActionDispatcher.Void OnActivated(MonoMac.Foundation.NSObject)] (MonoMac.Foundation.NSObject,MonoMac.ObjCRuntime.Selector,MonoMac.Foundation.NSObject) <0x000db>\n" +
        "at (wrapper managed-to-native) MonoMac.AppKit.NSApplication.NSApplicationMain (int,string[]) <0x00012>";

      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.ExposeParseStackTrace (stackTrace);

      Assert.AreEqual (3, result.Length);

      // Note that the leading "at" gets stripped
      Assert.IsNull (result [0].ClassName);
      Assert.AreEqual ("(wrapper dynamic-method) object.[MonoMac.AppKit.ActionDispatcher.Void OnActivated(MonoMac.Foundation.NSObject)] (MonoMac.Foundation.NSObject,MonoMac.ObjCRuntime.Selector,MonoMac.Foundation.NSObject) <0x00033>", result [0].FileName);
      Assert.IsNull (result [0].MethodName);
      Assert.AreEqual (0, result [0].LineNumber);

      Assert.IsNull (result [1].ClassName);
      Assert.AreEqual ("(wrapper native-to-managed) object.[MonoMac.AppKit.ActionDispatcher.Void OnActivated(MonoMac.Foundation.NSObject)] (MonoMac.Foundation.NSObject,MonoMac.ObjCRuntime.Selector,MonoMac.Foundation.NSObject) <0x000db>", result [1].FileName);
      Assert.IsNull (result [1].MethodName);
      Assert.AreEqual (0, result [1].LineNumber);

      Assert.IsNull (result [2].ClassName);
      Assert.AreEqual ("(wrapper managed-to-native) MonoMac.AppKit.NSApplication.NSApplicationMain (int,string[]) <0x00012>", result [2].FileName);
      Assert.IsNull (result [2].MethodName);
      Assert.AreEqual (0, result [2].LineNumber);
    }
  }
}

