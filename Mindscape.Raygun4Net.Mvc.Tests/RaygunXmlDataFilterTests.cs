using System.Collections.Generic;
using NUnit.Framework;
using Mindscape.Raygun4Net.Filters;
using System.Xml.Linq;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunXmlDataFilterTests : RaygunDataFilterTestsBaseFixture
  {
    [Test]
    public void CanParseValidXml()
    {
      Assert.That(new RaygunXmlDataFilter().CanParse("<root></root>"), Is.True);
    }

    [Test]
    public void CanNotParseInvalidXml()
    {
      Assert.That(new RaygunXmlDataFilter().CanParse("{}"), Is.False);
    }

    [Test]
    public void FilteringIsAppliedToElementsWithSensitiveKeysWithValuesIgnoringCase()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("BasicWithValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "PaSsWoRd" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(XDocument.Parse(filteredData), Is.Not.Null);// Check if it can still be parsed.
      Assert.That(filteredData, Is.EqualTo("<user><name>Ronald</name><password>[FILTERED]</password></user>"));
    }

    [Test]
    public void FilteringIsAppliedToElementsWithSensitiveKeysWithValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("BasicWithValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(XDocument.Parse(filteredData), Is.Not.Null);// Check if it can still be parsed.
      Assert.That(filteredData, Is.EqualTo("<user><name>Ronald</name><password>[FILTERED]</password></user>"));
    }

    [Test]
    public void FilteringIsNotAppliedToSensitiveKeysWithEmptyValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("BasicWithoutValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(XDocument.Parse(filteredData), Is.Not.Null);// Check if it can still be parsed.
      Assert.That(filteredData, Is.EqualTo("<user><name>Ronald</name><password></password></user>"));
    }

    [Test]
    public void FilteringIsAppliedToElementsWithAttributesWithSensitiveKeysWithValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("AttributedWithValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(XDocument.Parse(filteredData), Is.Not.Null);// Check if it can still be parsed.
      Assert.That(filteredData, Is.EqualTo("<root><raygunsettings apikey=\"123456\" /><user name=\"Raygun\" password=\"[FILTERED]\" /></root>"));
    }

    [Test]
    public void FilteringIsNotAppliedToElementsWithAttributesWithSensitiveKeysWithEmptyValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("AttributedWithoutValues.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(XDocument.Parse(filteredData), Is.Not.Null);// Check if it can still be parsed.
      Assert.That(filteredData, Is.EqualTo("<root><raygunsettings apikey=\"123456\" /><user name=\"Raygun\" password=\"\" /></root>"));
    }
  }
}
