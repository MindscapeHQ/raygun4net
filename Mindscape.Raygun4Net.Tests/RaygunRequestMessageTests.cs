using System.Collections.Generic;
using System.Linq;
using System.Web;

using Mindscape.Raygun4Net.Messages;

using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  /// <summary>
  /// Only test what we can when relying on HttpRequest.
  /// We cannot use HttpRequestBase without creating a .Net40
  /// specific dll as it moved from System.Web.Abstractions.dll to
  /// System.Web.dll in between 3.5 -> 4.0
  /// </summary>
  [TestFixture]
  public class RaygunRequestMessageTests
  {
    private HttpRequest _defaultRequest;

    [SetUp]
    public void SetUp()
    {
      _defaultRequest = new HttpRequest(string.Empty, "http://google.com", string.Empty);
    }

    [Test]
    public void HostNameTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, Enumerable.Empty<string>().ToList());

      Assert.That(message.HostName, Is.EqualTo("google.com"));
    }

    [Test]
    public void UrlTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, Enumerable.Empty<string>().ToList());

      Assert.That(message.Url, Is.EqualTo("/"));
    }

    [Test]
    public void HttpMethodTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, Enumerable.Empty<string>().ToList());

      Assert.That(message.HttpMethod, Is.EqualTo("GET"));
    }

    [Test]
    public void QueryStringTest()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");

      var message = new RaygunRequestMessage(request, Enumerable.Empty<string>().ToList());

      Assert.That(message.QueryString, Contains.Item(new KeyValuePair<string, string>("test", "test")));
    }
  }
}
