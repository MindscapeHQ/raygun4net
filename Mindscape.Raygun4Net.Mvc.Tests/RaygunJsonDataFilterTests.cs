using System.Collections.Generic;
using NUnit.Framework;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunJsonDataFilterTests
  {
    RaygunJsonDataFilter _filter;

    [SetUp]
    public void SetUp()
    {
      _filter = new RaygunJsonDataFilter();
    }

    [Test]
    public void SensitiveValueFilteredFromData()
    {
      var data = _filter.Filter("{\"Password\":\"ABC123\"}", new List<string> { "Password" });

      Assert.IsNotNull(data);
      Assert.AreEqual(data, "{\"Password\":\"[FILTERED]\"}");
    }
  }
}

