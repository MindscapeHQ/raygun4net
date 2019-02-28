using System;
using NUnit.Framework;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunRequestMessageBuilderTests
  {
    [SetUp]
    public void SetUp()
    {

    }

    [TearDown]
    public void TearDown()
    {

    }

    [Test]
    public void FilterASingleSensitiveValueFromDictionary()
    {
      var options = new RaygunRequestMessageOptions();
      options.AddSensitiveFieldNames("Password");

      var data = new Dictionary<string, string>() { { "UserName", "Raygun" }, { "Password", "pewpew"} };

      Assert.AreEqual(data.Count, 2);

      data = RaygunRequestMessageBuilder.FilterValues(data, options.IsSensitiveFieldIgnored) as Dictionary<string, string>;

      Assert.NotNull(data);
      Assert.AreEqual(data.Count, 1);
      Assert.AreEqual(data["UserName"], "Raygun");
    }

    [Test]
    public void FilterMultipleSensitiveValuesFromDictionary()
    {
      var options = new RaygunRequestMessageOptions();
      options.AddSensitiveFieldNames("Password");
      options.AddSensitiveFieldNames("IPAddress");

      var data = new Dictionary<string, string>() { { "UserName", "Raygun" }, { "Password", "pewpew" }, {"IPAddress", "1.1.1.1" } };

      Assert.AreEqual(data.Count, 3);

      data = RaygunRequestMessageBuilder.FilterValues(data, options.IsSensitiveFieldIgnored) as Dictionary<string, string>;

      Assert.NotNull(data);
      Assert.AreEqual(data.Count, 1);
      Assert.AreEqual(data["UserName"], "Raygun");
    }

    [Test]
    public void FilterMultipleSensitiveValuesFromDictionaryIgnoringCase()
    {
      var options = new RaygunRequestMessageOptions();
      options.AddSensitiveFieldNames("password");
      options.AddSensitiveFieldNames("ipAddress");

      var data = new Dictionary<string, string>() { { "UserName", "Raygun" }, { "Password", "pewpew" }, { "IPAddress", "1.1.1.1" } };

      Assert.AreEqual(data.Count, 3);

      data = RaygunRequestMessageBuilder.FilterValues(data, options.IsSensitiveFieldIgnored) as Dictionary<string, string>;

      Assert.NotNull(data);
      Assert.AreEqual(data.Count, 1);
      Assert.AreEqual(data["UserName"], "Raygun");
    }

    [Test]
    public void FilterJsonRawDataOfSensitiveValues()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var options = new RaygunRequestMessageOptions();
      options.AddSensitiveFieldNames("password");

      Assert.AreEqual(rawData.Length, 41);

      var filteredData = RaygunRequestMessageBuilder.FilterRawData(rawData, options);

      Assert.NotNull(filteredData);
      Assert.AreEqual(filteredData.Length, 21);
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
