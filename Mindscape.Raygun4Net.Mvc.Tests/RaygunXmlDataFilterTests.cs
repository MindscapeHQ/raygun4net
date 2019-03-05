using System.Collections.Generic;
using NUnit.Framework;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunXmlDataFilterTests : RaygunDataFilterTestsBaseFixture
  {
    [Test]
    public void FilterJsonRawDataOfSensitiveValues()
    {
      var filter = new RaygunXmlDataFilter();

      var xml = LoadPayload("basic.xml");

      var filteredData = filter.Filter(xml, new List<string>() { "password" });

      Assert.NotNull(filteredData);
    }
  }
}
