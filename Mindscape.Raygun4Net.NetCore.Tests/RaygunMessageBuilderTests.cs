using System;
using System.Linq;
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



    [Test]
    public void HasMachineName()
    {
      RaygunMessage message = _builder.SetMachineName(Environment.MachineName).Build();

      Assert.IsNotNull(message.Details);
      Assert.IsNotNull(message.Details.MachineName);

    }

    [Test]
    public void HasEnvironmentInformation()
    {
      RaygunMessage message = _builder.SetEnvironmentDetails().Build();

      Assert.IsNotNull(message.Details);
      Assert.IsNotNull(message.Details.Environment);
      Assert.IsNotEmpty(message.Details.Environment.Architecture);
      
      Assert.GreaterOrEqual(message.Details.Environment.WindowBoundsHeight, 0);
      Assert.GreaterOrEqual(message.Details.Environment.WindowBoundsWidth, 0);

      Assert.IsNotEmpty(message.Details.Environment.Cpu);

      Assert.GreaterOrEqual(message.Details.Environment.ProcessorCount, 1);
      Assert.IsNotEmpty(message.Details.Environment.OSVersion);
      Assert.IsNotEmpty(message.Details.Environment.Locale);

      Assert.IsNotNull(message.Details.Environment.DiskSpaceFree);
      Assert.True(message.Details.Environment.DiskSpaceFree.Any());
      Assert.True(message.Details.Environment.DiskSpaceFree.All(a => a > 0));
    }

    [Test]
    public void HasEnvironmentMemoryInformation()
    {
      RaygunMessage message = _builder.SetEnvironmentDetails().Build();

      Assert.NotZero(message.Details.Environment.AvailablePhysicalMemory);
      Assert.NotZero(message.Details.Environment.TotalPhysicalMemory);
      Assert.NotZero(message.Details.Environment.AvailableVirtualMemory);
      Assert.NotZero(message.Details.Environment.TotalVirtualMemory);
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
