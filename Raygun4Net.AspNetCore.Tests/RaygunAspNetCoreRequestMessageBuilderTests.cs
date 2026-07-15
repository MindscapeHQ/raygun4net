using Mindscape.Raygun4Net.AspNetCore.Builders;

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

[TestFixture]
public class RaygunAspNetCoreRequestMessageBuilderTests
{
  [Test]
  public void RequestIpAddressMaskingIsDisabledByDefault()
  {
    new RaygunSettings().IsRequestIpAddressMasked.Should().BeFalse();
  }

  [Test]
  public void IsRequestIpAddressMaskedBindsFromConfiguration()
  {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["RaygunSettings:IsRequestIpAddressMasked"] = "true"
      })
      .Build();

    var settings = new RaygunSettings();
    configuration.GetSection("RaygunSettings").Bind(settings);

    settings.IsRequestIpAddressMasked.Should().BeTrue();
  }

  [TestCase("192.168.12.123", 8123, false, "192.168.12.123:8123")]
  [TestCase("192.168.12.123", 8123, true, "192.168.12.0:8123")]
  [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", 0, false, "2001:db8:1234:5678:9abc:def0:1234:5678")]
  [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", 0, true, "2001:db8:1234::")]
  // Unmasked IPv6 keeps the historical addr:port shape for compatibility
  [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", 443, false, "2001:db8:1234:5678:9abc:def0:1234:5678:443")]
  // Masked IPv6 uses unambiguous [addr]:port formatting
  [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", 443, true, "[2001:db8:1234::]:443")]
  public async Task BuildAppliesConfiguredIpAddressMasking(string address, int port, bool isMasked, string expected)
  {
    var context = new DefaultHttpContext();
    context.Connection.RemoteIpAddress = IPAddress.Parse(address);
    context.Connection.RemotePort = port;
    var settings = new RaygunSettings
    {
      IsRequestIpAddressMasked = isMasked
    };

    var message = await RaygunAspNetCoreRequestMessageBuilder.Build(context, settings);

    message.IPAddress.Should().Be(expected);
  }

  [Test]
  public async Task ConfigurationBoundSettingsAreAppliedWhenBuildingRequestMessage()
  {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["RaygunSettings:IsRequestIpAddressMasked"] = "true"
      })
      .Build();

    var settings = new RaygunSettings();
    configuration.GetSection("RaygunSettings").Bind(settings);

    var context = new DefaultHttpContext();
    context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.12.123");
    context.Connection.RemotePort = 8123;

    var message = await RaygunAspNetCoreRequestMessageBuilder.Build(context, settings);

    message.IPAddress.Should().Be("192.168.12.0:8123");
  }

  [Test]
  public async Task MaskingRemovesKnownClientIpHeadersFromRequestMetadata()
  {
    var context = new DefaultHttpContext();
    context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.12.123");
    context.Request.Headers["X-Forwarded-For"] = "192.168.12.123";
    context.Request.Headers["Forwarded"] = "for=192.168.12.123";
    context.Request.Headers["X-Correlation-ID"] = "correlation-value";

    var message = await RaygunAspNetCoreRequestMessageBuilder.Build(context, new RaygunSettings
    {
      IsRequestIpAddressMasked = true
    });

    message.IPAddress.Should().Be("192.168.12.0");
    message.Headers.Contains("X-Forwarded-For").Should().BeFalse();
    message.Headers.Contains("Forwarded").Should().BeFalse();
    message.Headers["X-Correlation-ID"].Should().Be("correlation-value");
  }

  [Test]
  public async Task DisabledMaskingRetainsClientIpHeaders()
  {
    var context = new DefaultHttpContext();
    context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.12.123");
    context.Request.Headers["X-Forwarded-For"] = "192.168.12.123";

    var message = await RaygunAspNetCoreRequestMessageBuilder.Build(context, new RaygunSettings());

    message.Headers["X-Forwarded-For"].Should().Be("192.168.12.123");
  }

  // Any
  [TestCase("Banana", "*", true)]
  [TestCase("Apple", "*", true)]
  // Begins With
  [TestCase("Apple", "Ba*", false)]
  [TestCase("Banana", "Ba*", true)]
  // Ends With
  [TestCase("Apple", "*le", true)]
  [TestCase("Banana", "*le", false)]
  // Contains
  [TestCase("Apple", "*pl*", true)]
  [TestCase("Banana", "*pl*", false)]
  // Length Checks
  [TestCase("A", "**", false)]
  [TestCase("A", "*a*", true)]
  [TestCase("A", "*z*", false)]
  public void VerifyIgnoreScenariosWhenIgnoreKeyContainsStar(string key, string ignoredKey, bool expected)
  {
    RaygunAspNetCoreRequestMessageBuilder.IsIgnored(key, new[] { ignoredKey }).Should().Be(expected);
  }
  
  [TestCase("AppLe", "apple", true)]
  [TestCase("APPLE", "apple", true)]
  [TestCase("apple", "aPPle", true)]
  [TestCase("apple", "APPLE", true)]
  [TestCase("apple", "apple", true)]
  [TestCase("APPLE", "APPLE", true)]
  [TestCase("apple", "banana", false)]
  public void VerifyAbsoluteScenariosBasedOnCasing(string key, string ignoredKey, bool expected)
  {
    RaygunAspNetCoreRequestMessageBuilder.IsIgnored(key, new[] { ignoredKey }).Should().Be(expected);
  }
}
