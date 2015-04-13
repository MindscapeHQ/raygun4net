using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Tests.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.WindowsStore.Tests
{
  [TestFixture]
  public class RaygunErrorMessageBuilderTests
  {
    [Test]
    public void FormatGenericExceptionClassName()
    {
      var message = RaygunErrorMessageBuilder.Build(new GenericException<Dictionary<string, List<object>>>());
      Assert.AreEqual("Mindscape.Raygun4Net.Tests.Model.GenericException<Dictionary<String,List<Object>>>", message.ClassName);
    }

    [Test]
    public void IncludeNamespaceInExceptionClassName()
    {
      var message = RaygunErrorMessageBuilder.Build(new InvalidOperationException());
      Assert.AreEqual("System.InvalidOperationException", message.ClassName);
    }
  }
}
