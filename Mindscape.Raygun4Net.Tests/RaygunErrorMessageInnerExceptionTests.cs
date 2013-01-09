﻿using System;

using Mindscape.Raygun4Net.Messages;

using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
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
      Assert.That(() => new RaygunErrorMessage(_exception), Throws.Nothing);
    }

    [Test]
    public void ErrorMessageHasInnerError()
    {
      var errorMessage = new RaygunErrorMessage(_exception);

      Assert.That(errorMessage.InnerError, Is.Not.Null);
    }
  }
}
