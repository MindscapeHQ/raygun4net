using System;
using System.Collections.Generic;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net2.Tests
{
  [TestFixture]
  public class RaygunErrorMessageTests
  {
    private FakeRaygunErrorMessage _raygunErrorMessage;

    [SetUp]
    public void SetUp()
    {
      _raygunErrorMessage = new FakeRaygunErrorMessage();
    }

    [Test]
    public void NullStackTraceIsEmpty()
    {
      Assert.AreEqual(0, _raygunErrorMessage.GetStackTrace(null).Length);
    }

    [Test]
    public void NoStackTraceIsEmpty()
    {
      Assert.AreEqual(0, _raygunErrorMessage.GetStackTrace("").Length);
    }

    [Test]
    public void ExceptionBuilds()
    {
      Exception exception = null;
      try
      {
        ExceptionallyCrappyMethod<string, int>("bogus");
      }
      catch (Exception e)
      {
        exception = e;
      }

      Assert.That(() => new RaygunErrorMessage(exception), Throws.Nothing);
    }

    private void ExceptionallyCrappyMethod<T, T2>(T bung)
    {
      throw new InvalidOperationException();
    }

    [Test]
    public void ParseStackTrace()
    {
      string stackTrace = "   at Net2ErrorHarness.ChildClass.DontYouDareCrash() in e:\\Net2ErrorHarness\\BuggyClass.cs:line 21\r\n"
                        + "   at Net2ErrorHarness.BuggyClass.CallMethod() in e:\\Net2ErrorHarness\\BuggyClass.cs:line 14\r\n"
                        + "   at Net2ErrorHarness.Program.Main(String[] args) in e:\\Net2ErrorHarness\\Program.cs:line 16";
      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.GetStackTrace(stackTrace);

      Assert.AreEqual(3, result.Length);

      Assert.AreEqual("Net2ErrorHarness.ChildClass", result[0].ClassName);
      Assert.AreEqual("e:\\Net2ErrorHarness\\BuggyClass.cs", result[0].FileName);
      Assert.AreEqual(21, result[0].LineNumber);
      Assert.AreEqual("DontYouDareCrash()", result[0].MethodName);

      Assert.AreEqual("Net2ErrorHarness.BuggyClass", result[1].ClassName);
      Assert.AreEqual("e:\\Net2ErrorHarness\\BuggyClass.cs", result[1].FileName);
      Assert.AreEqual(14, result[1].LineNumber);
      Assert.AreEqual("CallMethod()", result[1].MethodName);

      Assert.AreEqual("Net2ErrorHarness.Program", result[2].ClassName);
      Assert.AreEqual("e:\\Net2ErrorHarness\\Program.cs", result[2].FileName);
      Assert.AreEqual(16, result[2].LineNumber);
      Assert.AreEqual("Main(String[] args)", result[2].MethodName);
    }

    [Test]
    public void ParseStackTraceWithGenericClass()
    {
      string stackTrace = "   at Net2ErrorHarness.ChildClass`1.DontYouDareCrash() in e:\\Net2ErrorHarness\\BuggyClass.cs:line 21";
      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.GetStackTrace(stackTrace);

      Assert.AreEqual(1, result.Length);

      Assert.AreEqual("Net2ErrorHarness.ChildClass`1", result[0].ClassName);
      Assert.AreEqual("e:\\Net2ErrorHarness\\BuggyClass.cs", result[0].FileName);
      Assert.AreEqual(21, result[0].LineNumber);
      Assert.AreEqual("DontYouDareCrash()", result[0].MethodName);
    }

    [Test]
    public void ParseStackTraceWithGenericMethod()
    {
      string stackTrace = "   at Net2ErrorHarness.ChildClass.DontYouDareCrash[T]() in e:\\Net2ErrorHarness\\BuggyClass.cs:line 21";
      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.GetStackTrace(stackTrace);

      Assert.AreEqual(1, result.Length);

      Assert.AreEqual("Net2ErrorHarness.ChildClass", result[0].ClassName);
      Assert.AreEqual("e:\\Net2ErrorHarness\\BuggyClass.cs", result[0].FileName);
      Assert.AreEqual(21, result[0].LineNumber);
      Assert.AreEqual("DontYouDareCrash[T]()", result[0].MethodName);
    }

    [Test]
    public void ParseStackTraceWithNoFileNameOrLineNumber()
    {
      string stackTrace = "   at System.Web.Util.CalliEventHandlerDelegateProxy.Callback(Object sender, EventArgs e)";
      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.GetStackTrace(stackTrace);

      Assert.AreEqual(1, result.Length);

      Assert.AreEqual("System.Web.Util.CalliEventHandlerDelegateProxy", result[0].ClassName);
      Assert.IsNull(result[0].FileName);
      Assert.AreEqual(0, result[0].LineNumber);
      Assert.AreEqual("Callback(Object sender, EventArgs e)", result[0].MethodName);
    }

    [Test]
    public void ParseStackTraceWithIntermediateMessage()
    {
      string stackTrace = "--- End of managed stack trace ---";
      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.GetStackTrace(stackTrace);

      Assert.AreEqual(1, result.Length);

      Assert.IsNull(result[0].ClassName);
      Assert.AreEqual("--- End of managed stack trace ---", result[0].FileName);
      Assert.AreEqual(0, result[0].LineNumber);
      Assert.IsNull(result[0].MethodName);
    }
  }
}
