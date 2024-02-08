using System.Collections.Generic;
using NUnit.Framework;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunKeyValuePairDataFilterTests : RaygunDataFilterTestsBaseFixture
  {
    [Test]
    public void CanParseValidKeyValuePairs()
    {
      Assert.That(new RaygunKeyValuePairDataFilter().CanParse("key=value"), Is.True);
    }

    [Test]
    public void CanNotParseInvalidKeyValuePairs()
    {
      Assert.That(new RaygunXmlDataFilter().CanParse("{}"), Is.False);
    }

    [Test]
    public void DataRemainsUnchangedWhenSensitiveKeysAreNotFound()
    {
      var filter = new RaygunKeyValuePairDataFilter();

      var rawData = "key=value";

      var filteredData = filter.Filter(rawData, new List<string>() { "password" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(filteredData, Is.EqualTo("key=value"));
    }

    [Test]
    public void FilteringIsAppliedToPairsWithSensitiveKeys()
    {
      var filter = new RaygunKeyValuePairDataFilter();

      var rawData = "user=raygun&password=ABC123";

      var filteredData = filter.Filter(rawData, new List<string>() { "password" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(filteredData, Is.EqualTo("user=raygun&password=[FILTERED]"));
    }

    [Test]
    public void FilteringIsAppliedToPairsWithSensitiveKeysWhileIgnoringCase()
    {
      var filter = new RaygunKeyValuePairDataFilter();

      var rawData = "user=raygun&password=ABC123";

      var filteredData = filter.Filter(rawData, new List<string>() { "PaSsWoRd" });

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(filteredData, Is.EqualTo("user=raygun&password=[FILTERED]"));
    }
  }
}
