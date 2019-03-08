using System.Collections.Generic;
using NUnit.Framework;
using Mindscape.Raygun4Net.Filters;
using System.Linq;
using System.Xml.Linq;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunXmlDataFilterTests : RaygunDataFilterTestsBaseFixture
  {
    [Test]
    public void CanParseValidXml()
    {
      Assert.True(new RaygunXmlDataFilter().CanParse("<root></root>"));
    }

    [Test]
    public void CanNotParseInvalidXml()
    {
      Assert.False(new RaygunXmlDataFilter().CanParse("{}"));
    }

    [Test]
    public void FilteringIsAppliedToElementsWithSensitiveKeysWithValuesIgnoringCase()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("BasicWithValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "PaSsWoRd" });

      Assert.NotNull(filteredData);
      Assert.NotNull(XDocument.Parse(filteredData));// Check if it can still be parsed.
      Assert.AreEqual(filteredData, "<user>\n  <name>Ronald</name>\n  <password>[FILTERED]</password>\n</user>");
    }

    [Test]
    public void FilteringIsAppliedToElementsWithSensitiveKeysWithValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("BasicWithValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.NotNull(filteredData);
      Assert.NotNull(XDocument.Parse(filteredData));// Check if it can still be parsed.
      Assert.AreEqual(filteredData, "<user>\n  <name>Ronald</name>\n  <password>[FILTERED]</password>\n</user>");
    }

    [Test]
    public void FilteringIsNotAppliedToSensitiveKeysWithEmptyValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("BasicWithoutValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.NotNull(filteredData);
      Assert.NotNull(XDocument.Parse(filteredData));// Check if it can still be parsed.
      Assert.AreEqual(filteredData, "<user>\n  <name>Ronald</name>\n  <password></password>\n</user>");
    }

    [Test]
    public void FilteringIsAppliedToElementsWithAttributesWithSensitiveKeysWithValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("AttributedWithValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.NotNull(filteredData);
      Assert.NotNull(XDocument.Parse(filteredData));// Check if it can still be parsed.
      Assert.AreEqual(filteredData, "<root>\n  <raygunsettings apikey=\"123456\" />\n  <user name=\"Raygun\" password=\"[FILTERED]\" />\n</root>");
    }

    [Test]
    public void FilteringIsNotAppliedToElementsWithAttributesWithSensitiveKeysWithEmptyValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("AttributedWithoutValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.NotNull(filteredData);
      Assert.NotNull(XDocument.Parse(filteredData));// Check if it can still be parsed.
      Assert.AreEqual(filteredData, "<root>\n  <raygunsettings apikey=\"123456\" />\n  <user name=\"Raygun\" password=\"\" />\n</root>");
    }
  }
}
