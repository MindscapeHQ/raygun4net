using System;
using NUnit.Framework;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Mvc.Tests
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

      Assert.AreEqual(rawData.Length, 15);

      var filteredData = RaygunRequestMessageBuilder.StripSensitiveValues(rawData, options);

      Assert.NotNull(filteredData);
      Assert.AreEqual(filteredData.Length, 15);
      Assert.AreEqual(filteredData, "I am unchanged!");
    }

    [Test]
    public void DataContainsReturnsTrueWhenMatchingOnExactCase()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var containsSensitiveData = RaygunRequestMessageBuilder.DataContains(rawData, new List<string>() { "Password" });

      Assert.AreEqual(containsSensitiveData, true);
    }

    [Test]
    public void DataContainsReturnsFalseWhenCaseDoesNotMatch()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var containsSensitiveData = RaygunRequestMessageBuilder.DataContains(rawData, new List<string>() { "password" });

      Assert.AreEqual(containsSensitiveData, false);
    }
  }
}
