using System;
using System.Collections.Generic;
using System.Text;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net2.Tests
{
  [TestFixture]
  public class RaygunErrorMessageInnerExceptionTests
  {
    private Exception _exception;

    [SetUp]
    public void SetUp()
    {
      try
      {
        ExceptionallyCrappyMethod<string, int>("bogus");
      }
      catch (Exception ex)
      {
        _exception = ex;
      }
    }

    private void ExceptionallyCrappyMethod<T, T2>(T bung)
    {
      try
      {
        throw new NotImplementedException();
      }
      catch (Exception exception)
      {
        throw new InvalidOperationException("Inner Exception!", exception);
      }
    }

    [Test]
    public void ExceptionBuilds()
    {
      Assert.That(() => RaygunErrorMessageBuilder.Build(_exception), Throws.Nothing);
    }

    [Test]
    public void ErrorMessageHasInnerError()
    {
      var errorMessage = RaygunErrorMessageBuilder.Build(_exception);

      Assert.That(errorMessage.InnerError, Is.Not.Null);
    }
  }
}
