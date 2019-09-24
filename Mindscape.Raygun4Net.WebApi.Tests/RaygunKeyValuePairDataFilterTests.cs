using System.Collections.Generic;
using NUnit.Framework;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.WebApi.Tests
{
  [TestFixture]
  public class RaygunKeyValuePairDataFilterTests : RaygunDataFilterTestsBaseFixture
  {
    [Test]
    public void CanParseValidKeyValuePairs()
    {
      Assert.True(new RaygunKeyValuePairDataFilter().CanParse("key=value"));
    }

    [Test]
    public void CanNotParseInvalidKeyValuePairs()
    {
      Assert.False(new RaygunXmlDataFilter().CanParse("{}"));
    }

    [Test]
    public void DataRemainsUnchangedWhenSensitiveKeysAreNotFound()
    {
      var filter = new RaygunKeyValuePairDataFilter();

      var rawData = "key=value";

      var filteredData = filter.Filter(rawData, new List<string>() { "password" });

      Assert.NotNull(filteredData);
      Assert.AreEqual(filteredData, "key=value");
    }

    [Test]
    public void FilteringIsAppliedToPairsWithSensitiveKeys()
    {
      var filter = new RaygunKeyValuePairDataFilter();

      var rawData = "user=raygun&password=ABC123";

      var filteredData = filter.Filter(rawData, new List<string>() { "password" });

      Assert.NotNull(filteredData);
      Assert.AreEqual(filteredData, "user=raygun&password=[FILTERED]");
    }

    [Test]
    public void FilteringIsAppliedToPairsWithSensitiveKeysWhileIgnoringCase()
    {
      var filter = new RaygunKeyValuePairDataFilter();

      var rawData = "user=raygun&password=ABC123";

      var filteredData = filter.Filter(rawData, new List<string>() { "PaSsWoRd" });

      Assert.NotNull(filteredData);
      Assert.AreEqual(filteredData, "user=raygun&password=[FILTERED]");
    }
  }
}
