using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.WinRT.Tests
{
  [TestFixture]
  public class RaygunMessageBuilderTests
  {
    private RaygunMessageBuilder _builder;

    [SetUp]
    public void SetUp()
    {
      _builder = RaygunMessageBuilder.New;
    }

    [Test]
    public void New()
    {
      Assert.IsNotNull(_builder);
    }

    [Test]
    public void SetVersion()
    {
      IRaygunMessageBuilder builder = _builder.SetVersion("Custom Version");
      Assert.AreEqual(_builder, builder);

      RaygunMessage message = _builder.Build();
      Assert.AreEqual("Custom Version", message.Details.Version);
    }

    [Test]
    public void SetVersion_Null()
    {
      _builder.SetVersion(null);
      RaygunMessage message = _builder.Build();
      Assert.AreEqual("Not Provided", message.Details.Version);
    }
  }
}
