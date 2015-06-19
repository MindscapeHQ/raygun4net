using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

      Assert.AreEqual(2, message.InnerErrors.Count());
      Assert.AreEqual("System.IO.FileNotFoundException", message.InnerErrors[0].ClassName);
      Assert.AreEqual("System.OutOfMemoryException", message.InnerErrors[1].ClassName);

      Assert.IsTrue(message.InnerErrors[0].Data["Type"].ToString().Contains("FakeRaygunClient"));
      Assert.IsTrue(message.InnerErrors[1].Data["Type"].ToString().Contains("WrapperException"));
    }
  }
}
