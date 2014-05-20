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
    public void ParseStackTrace()
    {
      string stackTrace = "";
      RaygunErrorStackTraceLineMessage[] result = _raygunErrorMessage.GetStackTrace(stackTrace);
    }
  }
}
