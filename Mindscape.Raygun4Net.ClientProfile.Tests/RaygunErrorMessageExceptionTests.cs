using System;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunErrorMessageExceptionTests
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
      throw new InvalidOperationException();
    }

    [Test]
    public void ExceptionBuilds()
    {
      Assert.That(() => RaygunErrorMessageBuilder.Build(_exception), Throws.Nothing);
    }
  }
}
