using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using NUnit.Framework;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Xamarin.Android.Tests.Builders
{
  [TestFixture]
  public class RaygunErrorMessageBuilderTests
  {
    [Test]
    public void ParseStackTraceLine_MemoryAddressInSquareBrackets_ClassMethodFileAndLineNumberAreObtained()
    {
      string stackTraceLine = "at Raygun.Trigger.Pull () [0x00000] in Raygun.Trigger.cs:15";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Raygun.Trigger", message.ClassName);
      Assert.AreEqual("Pull()", message.MethodName);
      Assert.AreEqual("Raygun.Trigger.cs", message.FileName);
      Assert.AreEqual(15, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_MemoryAddressInAngledBrackets_ClassMethodFileAndLineNumberAreObtained()
    {
      string stackTraceLine = "at Raygun.Trigger.Pull () <0x00000> in Raygun.Trigger.cs:15";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Raygun.Trigger", message.ClassName);
      Assert.AreEqual("Pull()", message.MethodName);
      Assert.AreEqual("Raygun.Trigger.cs", message.FileName);
      Assert.AreEqual(15, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_OnlyClassAndMethod_FileAndLineNumberIsNull()
    {
      string stackTraceLine = "at Raygun.Trigger.Pull(int count)";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.IsNull(message.FileName);
      Assert.IsNull(message.LineNumber);

      Assert.AreEqual("Raygun.Trigger", message.ClassName);
      Assert.AreEqual("Pull(int count)", message.MethodName);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_FileAndNumberInBrackets_FileAndLineNumberAreObtained()
    {
      string stackTraceLine = "at Raygun.Trigger.Pull(Raygun.Trigger.cs:15)";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Raygun.Trigger", message.ClassName);
      Assert.AreEqual("Pull", message.MethodName);
      Assert.AreEqual("Raygun.Trigger.cs", message.FileName);
      Assert.AreEqual(15, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_GenericMethod_GenericParameterIsIncludedInMethodName()
    {
      string stackTraceLine = "at Raygun.Trigger.Pull[T] () in Raygun.Trigger.cs:15";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Raygun.Trigger", message.ClassName);
      Assert.AreEqual("Pull[T]()", message.MethodName);
      Assert.AreEqual("Raygun.Trigger.cs", message.FileName);
      Assert.AreEqual(15, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_AsyncMethod_MethodIsObtainedFromAngledBrackets()
    {
      string stackTraceLine = "at Raygun.Trigger+<PullAsync>d__7.MoveNext () in Raygun.Trigger.cs:15";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Raygun.Trigger", message.ClassName);
      Assert.AreEqual("PullAsync()", message.MethodName);
      Assert.AreEqual("Raygun.Trigger.cs", message.FileName);
      Assert.AreEqual(15, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_AsyncMethodMultipleAngleBrackets_MethodIsObtainedFromAngledBrackets()
    {
      string stackTraceLine = "at Android.App.SyncContext+<Post>c__AnonStorey0.<>m__0 () [0x00000] in <2e3d0b54edd14877b2091b405b48598f>:0";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Android.App.SyncContext", message.ClassName);
      Assert.AreEqual("Post()", message.MethodName);
      Assert.AreEqual("<2e3d0b54edd14877b2091b405b48598f>", message.FileName);
      Assert.AreEqual(0, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_Constructor_MethodNameStartsWithPeriod()
    {
      string stackTraceLine = "at Raygun..ctor () in Raygun.cs:7";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("Raygun", message.ClassName);
      Assert.AreEqual(".ctor()", message.MethodName);
      Assert.AreEqual("Raygun.cs", message.FileName);
      Assert.AreEqual(7, message.LineNumber);
      Assert.AreEqual(stackTraceLine, message.Raw);
    }

    [Test]
    public void ParseStackTraceLine_WrapperDynamicMethod_LineIsLoggedInRawProperty()
    {
      string stackTraceLine = "at (wrapper dynamic-method) System.Object:6b4f2c61-c425-4660-9dd5-ef82ab5b5664 (intptr,intptr)";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("at (wrapper dynamic-method) System.Object:6b4f2c61-c425-4660-9dd5-ef82ab5b5664 (intptr,intptr)", message.Raw);

      Assert.IsNull(message.ClassName);
      Assert.IsNull(message.MethodName);
      Assert.IsNull(message.FileName);
      Assert.IsNull(message.LineNumber);
    }

    [Test]
    public void ParseStackTraceLine_ArbitraryText_LineIsLoggedInRawProperty()
    {
      string stackTraceLine = "--- End of managed Java.Lang.ReflectiveOperationException stack trace ---";

      var message = RaygunErrorMessageBuilder.ParseStackTraceLine(stackTraceLine);

      Assert.AreEqual("--- End of managed Java.Lang.ReflectiveOperationException stack trace ---", message.Raw);

      Assert.IsNull(message.ClassName);
      Assert.IsNull(message.MethodName);
      Assert.IsNull(message.FileName);
      Assert.IsNull(message.LineNumber);
    }
  }
}