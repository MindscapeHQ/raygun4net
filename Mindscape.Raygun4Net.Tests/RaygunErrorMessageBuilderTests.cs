using System;

using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunErrorMessageBuilderTests
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
    public void ClassNameIsNotEmpty()
    {
      var raygunErrorMessage = RaygunErrorMessageBuilder.New.SetClassName(_exception).Build();

      Assert.That(raygunErrorMessage.ClassName, Is.Not.EqualTo(string.Empty));
    }

    [Test]
    public void StackTraceIsNotEmpty()
    {
      var raygunErrorMessage = RaygunErrorMessageBuilder.New.SetStackTrace(_exception).Build();

      Assert.That(raygunErrorMessage.StackTrace, Is.Not.Empty);
    }

    [Test]
    public void MessageIsNotEmpty()
    {
      var raygunErrorMessage = RaygunErrorMessageBuilder.New.SetMessage(_exception).Build();

      Assert.That(raygunErrorMessage.Message, Is.Not.EqualTo(string.Empty));
    }
  }
}