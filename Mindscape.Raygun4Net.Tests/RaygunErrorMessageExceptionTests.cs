using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Tests.Model;
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

    [Test]
    public void FormatGenericExceptionClassName()
    {
      var message = RaygunErrorMessageBuilder.Build(new GenericException<Dictionary<string, List<object>>>());
      Assert.That("Mindscape.Raygun4Net.Tests.Model.GenericException<Dictionary<String,List<Object>>>", Is.EqualTo(message.ClassName));
    }

    [Test]
    public void IncludeNamespaceInExceptionClassName()
    {
      var message = RaygunErrorMessageBuilder.Build(_exception);
      Assert.That("System.InvalidOperationException", Is.EqualTo(message.ClassName));
    }
  }
}
