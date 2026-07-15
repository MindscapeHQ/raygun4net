using NUnit.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using Mindscape.Raygun4Net.WebApi.Builders;

namespace Mindscape.Raygun4Net.WebApi.Tests
{
  [TestFixture]
  public class RaygunRequestMessageBuilderTests
  {
    [TestCase("192.168.12.123", false, "192.168.12.123")]
    [TestCase("192.168.12.123", true, "192.168.12.0")]
    [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", true, "2001:db8:1234::")]
    public void BuildAppliesConfiguredIpAddressMasking(string address, bool isMasked, string expected)
    {
      using (var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com"))
      {
        request.Content = new StringContent(string.Empty);
        dynamic context = new ExpandoObject();
        context.Request = new ExpandoObject();
        context.Request.UserHostAddress = address;
        request.Properties["MS_HttpContext"] = context;
        var options = new RaygunRequestMessageOptions
        {
          IsRequestIpAddressMasked = isMasked
        };

        var message = RaygunWebApiRequestMessageBuilder.Build(request, options);

        Assert.That(message.IPAddress, Is.EqualTo(expected));
      }
    }

    [Test]
    public void BuildRemovesKnownClientIpHeadersWhenMaskingIsEnabled()
    {
      using (var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com"))
      {
        request.Content = new StringContent(string.Empty);
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "192.168.12.123");
        request.Headers.TryAddWithoutValidation("Forwarded", "for=192.168.12.123");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "correlation-value");
        dynamic context = new ExpandoObject();
        context.Request = new ExpandoObject();
        context.Request.UserHostAddress = "192.168.12.123";
        request.Properties["MS_HttpContext"] = context;

        var message = RaygunWebApiRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions
        {
          IsRequestIpAddressMasked = true
        });

        Assert.That(message.Headers.Contains("X-Forwarded-For"), Is.False);
        Assert.That(message.Headers.Contains("Forwarded"), Is.False);
        Assert.That(message.Headers["X-Correlation-ID"], Is.EqualTo("correlation-value"));
      }
    }

    [Test]
    public void BuildRetainsClientIpHeadersWhenMaskingIsDisabled()
    {
      using (var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com"))
      {
        request.Content = new StringContent(string.Empty);
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "192.168.12.123");
        dynamic context = new ExpandoObject();
        context.Request = new ExpandoObject();
        context.Request.UserHostAddress = "192.168.12.123";
        request.Properties["MS_HttpContext"] = context;

        var message = RaygunWebApiRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

        Assert.That(message.Headers["X-Forwarded-For"], Is.EqualTo("192.168.12.123"));
      }
    }

    [Test]
    public void RawDataRemainsUnchangedWhenParsingFails()
    {
      var rawData = "I am unchanged!";

      var options = new RaygunRequestMessageOptions();
      options.AddSensitiveFieldNames("password");

      Assert.That(rawData.Length, Is.EqualTo(15));

      var filteredData = RaygunWebApiRequestMessageBuilder.StripSensitiveValues(rawData, options);

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(filteredData.Length, Is.EqualTo(15));
      Assert.That(filteredData, Is.EqualTo("I am unchanged!"));
    }

    [Test]
    public void DataContainsReturnsTrueWhenMatchingOnExactCase()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var containsSensitiveData = RaygunWebApiRequestMessageBuilder.DataContains(rawData, new List<string>() { "Password" });

      Assert.That(containsSensitiveData, Is.EqualTo(true));
    }

    [Test]
    public void DataContainsReturnsFalseWhenCaseDoesNotMatch()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var containsSensitiveData = RaygunWebApiRequestMessageBuilder.DataContains(rawData, new List<string>() { "password" });

      Assert.That(containsSensitiveData, Is.EqualTo(false));
    }
  }
}
