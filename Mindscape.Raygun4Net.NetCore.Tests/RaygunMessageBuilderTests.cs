using System;
using System.Collections.Generic;
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
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
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
      var builder = _builder.SetVersion("Custom Version");
      Assert.That(_builder, Is.EqualTo(builder));

      var message = _builder.Build();
      Assert.That("Custom Version", Is.EqualTo(message.Details.Version));
    }

    [Test]
    public void SetTimeStamp()
    {
      var time = new DateTime(2015, 2, 16);
      var message = _builder.SetTimeStamp(time).Build();
      Assert.That(time, Is.EqualTo(message.OccurredOn));
    }

    [Test]
    public void SetNullTimeStamp()
    {
      var message = _builder.SetTimeStamp(null).Build();
      Assert.That((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1, Is.True);
    }

    [Test]
    public void HasMachineName()
    {
      var message = _builder.SetMachineName(Environment.MachineName).Build();

      Assert.That(message.Details, Is.Not.Null);
      Assert.That(message.Details.MachineName, Is.Not.Null);
    }

    [Test]
    public void HasEnvironmentInformation()
    {
      var message = _builder.SetEnvironmentDetails().Build();

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
      var message = _builder.SetEnvironmentDetails().Build();

      Assert.That(message.Details.Environment.AvailablePhysicalMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.TotalPhysicalMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.AvailableVirtualMemory, Is.Not.Zero);
      Assert.That(message.Details.Environment.TotalVirtualMemory, Is.Not.Zero);
    }

    // Response tests

    [Test]
    public void ResponseIsNullForNonWebExceptions()
    {
      var exception = new NullReferenceException("The thing is null");
      _builder.SetExceptionDetails(exception);
      var message = _builder.Build();
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
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_ExactMatch()
    {
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "PATH"
        }
      };
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>().Should().Contain(s => s.Equals("path", StringComparison.OrdinalIgnoreCase));
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_StartsWith()
    {
      Environment.SetEnvironmentVariable("TEST_One", "1");
      Environment.SetEnvironmentVariable("TEST_Two", "2");
      Environment.SetEnvironmentVariable("TEST_Three", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "TEST_*"
        }
      };
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(3)
         .And.Contain(new []
      {
        "TEST_One", 
        "TEST_Two", 
        "TEST_Three"
      });
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_EndsWith()
    {
      Environment.SetEnvironmentVariable("One_Banana", "1");
      Environment.SetEnvironmentVariable("Two_Banana", "2");
      Environment.SetEnvironmentVariable("Three_Banana", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "*_Banana"
        }
      };
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(3)
         .And.Contain(new []
      {
        "One_Banana", 
        "Two_Banana", 
        "Three_Banana"
      });
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_Contains()
    {
      Environment.SetEnvironmentVariable("ONE_Banana_Two", "1");
      Environment.SetEnvironmentVariable("Two_Test_Three", "2");
      Environment.SetEnvironmentVariable("ThreeBananaFour", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "*_Banana*"
        }
      };
      
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(1)
         .And.Contain(new []
      {
        "ONE_Banana_Two"
      });
    }
    
    [Test]
    public void SetEnvironmentDetails_WithEnvironmentVariables_Star_ShouldReturnNothing()
    {
      Environment.SetEnvironmentVariable("ONE_Banana_Two", "1");
      Environment.SetEnvironmentVariable("Two_Test_Three", "2");
      Environment.SetEnvironmentVariable("ThreeBananaFour", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          "*",
          "**",
          "***",
          "* *",
        }
      };
      
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(0);
    }
    
    [TestCase("LEMON", "lemon")]
    [TestCase("kIwIfRuIt", "KIWIFRUIT")]
    [TestCase("WAterMeLON", "water*")]
    [TestCase("gRaPE", "*ape")]
    [TestCase("DraGonFrUiT", "*nfr*")]
    public void SetEnvironmentDetails_WithEnvironmentVariablesWithDifferentCasing_ShouldIgnoreCaseAndReturn(string key, string search)
    {
      Environment.SetEnvironmentVariable("lOnGan", "1");
      Environment.SetEnvironmentVariable(key, "2");
      Environment.SetEnvironmentVariable("aPrIcOt", "3");
      
      var settings = new RaygunSettings
      {
        EnvironmentVariables = new List<string>
        {
          search
        }
      };
      
      RaygunEnvironmentMessageBuilder.LastUpdate = DateTime.MinValue;
      var builder = RaygunMessageBuilder.New(settings)
                                        .SetEnvironmentDetails();
      
      var msg = builder.Build();

      msg.Details.Environment.EnvironmentVariables.Keys.Cast<string>()
         .Should().HaveCount(1)
         .And.Contain(new []
         {
           key
         });
    }
  }
}
