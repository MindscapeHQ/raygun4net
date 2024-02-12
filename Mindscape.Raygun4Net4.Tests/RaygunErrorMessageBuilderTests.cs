using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net4.Tests
{
  [TestFixture]
  public class RaygunErrorMessageBuilderTests
  {
    [Test]
    public void ReflectionTypeLoadExceptionSupport()
    {
      FileNotFoundException ex1 = new FileNotFoundException();
      OutOfMemoryException ex2 = new OutOfMemoryException();
      ReflectionTypeLoadException wrapper = new ReflectionTypeLoadException(new Type[] { typeof(FakeRaygunClient), typeof(WrapperException) }, new Exception[] { ex1, ex2 });

      RaygunErrorMessage message = RaygunErrorMessageBuilder.Build(wrapper);

      Assert.That(2, Is.EqualTo(message.InnerErrors.Count()));
      Assert.That("System.IO.FileNotFoundException", Is.EqualTo(message.InnerErrors[0].ClassName));
      Assert.That("System.OutOfMemoryException", Is.EqualTo(message.InnerErrors[1].ClassName));

      Assert.That(message.InnerErrors[0].Data["Type"].ToString().Contains("FakeRaygunClient"), Is.True);
      Assert.That(message.InnerErrors[1].Data["Type"].ToString().Contains("WrapperException"), Is.True);
    }
  }
}
