using System;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class RaygunMessageBuilderTests
  {
    private RaygunSettings _settings;
    private RaygunMessageBuilder _builder;

    [SetUp]
    public void SetUp()
    {
      _settings = new RaygunSettings();
      _builder = RaygunMessageBuilder.New(_settings);
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
    public void SetTimeStamp()
    {
      DateTime time = new DateTime(2015, 2, 16);
      RaygunMessage message = _builder.SetTimeStamp(time).Build();
      Assert.AreEqual(time, message.OccurredOn);
    }

    [Test]
    public void SetNullTimeStamp()
    {
      RaygunMessage message = _builder.SetTimeStamp(null).Build();
      Assert.IsTrue((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1);
    }

    // Response tests

    [Test]
    public void ResponseIsNullForNonWebExceptions()
    {
      NullReferenceException exception = new NullReferenceException("The thing is null");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.IsNull(message.Details.Response);
    }
  }
}
