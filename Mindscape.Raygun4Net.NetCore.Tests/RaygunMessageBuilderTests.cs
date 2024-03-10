using System;
using System.Linq;

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
      Assert.That(_builder, Is.Not.Null);
    }

    [Test]
    public void SetVersion()
    {
      IRaygunMessageBuilder builder = _builder.SetVersion("Custom Version");
      Assert.That(_builder, Is.EqualTo(builder));

      RaygunMessage message = _builder.Build();
      Assert.That("Custom Version", Is.EqualTo(message.Details.Version));
    }

    [Test]
    public void SetTimeStamp()
    {
      DateTime time = new DateTime(2015, 2, 16);
      RaygunMessage message = _builder.SetTimeStamp(time).Build();
      Assert.That(time, Is.EqualTo(message.OccurredOn));
    }

    [Test]
    public void SetNullTimeStamp()
    {
      RaygunMessage message = _builder.SetTimeStamp(null).Build();
      Assert.That((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1, Is.True);
    }



    [Test]
    public void HasMachineName()
    {
      RaygunMessage message = _builder.SetMachineName(Environment.MachineName).Build();

      Assert.That(message.Details, Is.Not.Null);
      Assert.That(message.Details.MachineName, Is.Not.Null);

    }

    [Test]
    public void HasEnvironmentInformation()
    {
      RaygunMessage message = _builder.SetEnvironmentDetails().Build();

      Assert.That(message.Details, Is.Not.Null);
      Assert.That(message.Details.Environment, Is.Not.Null);
      Assert.That(message.Details.Environment.Architecture, Is.Not.Empty);
      
      Assert.That(message.Details.Environment.WindowBoundsHeight, Is.GreaterThanOrEqualTo(0));
      Assert.That(message.Details.Environment.WindowBoundsWidth, Is.GreaterThanOrEqualTo(0));

      Assert.That(message.Details.Environment.Cpu, Is.Not.Empty);

      Assert.That(message.Details.Environment.ProcessorCount, Is.GreaterThanOrEqualTo(1));
      Assert.That(message.Details.Environment.OSVersion, Is.Not.Empty);
      Assert.That(message.Details.Environment.Locale, Is.Not.Empty);

      Assert.That(message.Details.Environment.DiskSpaceFree, Is.Not.Null);
      Assert.That(message.Details.Environment.DiskSpaceFree.Any(), Is.True);
      Assert.That(message.Details.Environment.DiskSpaceFree.All(a => a > 0), Is.True);
    }

    [Test]
    public void HasEnvironmentMemoryInformation()
    {
      RaygunMessage message = _builder.SetEnvironmentDetails().Build();

      Assert.That(message.Details.Environment.AvailablePhysicalMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.TotalPhysicalMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.AvailableVirtualMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.TotalVirtualMemory, Is.Not.Zero);
    }

    // Response tests

    [Test]
    public void ResponseIsNullForNonWebExceptions()
    {
      NullReferenceException exception = new NullReferenceException("The thing is null");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Response, Is.Null);
    }
    
    [Test]
    public void Customise_ExistingMessage_CorrectlyModifiesProperties()
    {
      var settings = new RaygunSettings();
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetVersion("1.0.0")
                                        .SetEnvironmentDetails()
                                        .Customise(m =>
                                        {
                                          m.Details.Version = "2.0.0";
                                          m.Details.Environment.Architecture = "BANANA";
                                        });
      
      var modifiedMessage = builder.Build();

      modifiedMessage.Details.Version.Should().Be("2.0.0");
      modifiedMessage.Details.Environment.Architecture.Should().Be("BANANA");
    }
  }
}
