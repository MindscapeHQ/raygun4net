using NUnit.Framework;
using System.Collections.Generic;
using Mindscape.Raygun4Net.WebApi.Builders;

namespace Mindscape.Raygun4Net.WebApi.Tests
{
  [TestFixture]
  public class RaygunRequestMessageBuilderTests
  {
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
