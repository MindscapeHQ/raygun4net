using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
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
  #if DEBUG
  #endif
      _builder.SetVersion(null);
      RaygunMessage message = _builder.Build();
      Assert.AreEqual("Not supplied", message.Details.Version);
    }
  }
}
