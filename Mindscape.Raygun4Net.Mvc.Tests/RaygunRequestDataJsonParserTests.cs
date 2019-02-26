using NUnit.Framework;
using Mindscape.Raygun4Net.Parsers;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunRequestDataJsonParserTests
  {
    RaygunRequestDataJsonParser _parser;

    [SetUp]
    public void SetUp()
    {
      _parser = new RaygunRequestDataJsonParser();
    }

    [Test]
    public void ParseJsonContainingStringValues()
    {
      var data = "{\"MyKey\":\"MyValue\"}";
      var dict = _parser.ToDictionary(data);

      Assert.IsNotNull(dict);
      Assert.AreEqual(dict.Count, 1);
    }

    [Test]
    public void ParseJsonContainingNonStringValues()
    {
      var data = "{\"Url\":\"http://thing.com:1234\",\"ProjectId\":null,\"Enabled\":false}";
      var dict = _parser.ToDictionary(data);

      Assert.IsNotNull(dict);
      Assert.AreEqual(dict.Count, 3);
      Assert.AreEqual(dict["Url"], "http://thing.com:1234");
      Assert.AreEqual(dict["ProjectId"], null);
      Assert.AreEqual(dict["Enabled"], false);
    }

  }
}

