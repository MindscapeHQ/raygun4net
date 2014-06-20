using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
      var message = new RaygunRequestMessage(_defaultRequest, new RaygunRequestMessageOptions());

      Assert.That(message.HostName, Is.EqualTo("google.com"));
    }

    [Test]
    public void UrlTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, new RaygunRequestMessageOptions());

      Assert.That(message.Url, Is.EqualTo("/"));
    }

    [Test]
    public void HttpMethodTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, new RaygunRequestMessageOptions());

      Assert.That(message.HttpMethod, Is.EqualTo("GET"));
    }

    [Test]
    public void QueryStringTest()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");

      var message = new RaygunRequestMessage(request, new RaygunRequestMessageOptions());

      Assert.That(message.QueryString, Contains.Item(new KeyValuePair<string, string>("test", "test")));
    }

    // Form data

    [Test]
    public void FormData()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormData1", "FormDataValue");
      request.Form.Add("TestFormData2", "FormDataValue");
      request.Form.Add("TestFormData3", "FormDataValue");
      Assert.AreEqual(3, request.Form.Count);

      var message = new RaygunRequestMessage(request, new RaygunRequestMessageOptions());

      Assert.AreEqual(3, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormData1"));
      Assert.IsTrue(message.Form.Contains("TestFormData2"));
      Assert.IsTrue(message.Form.Contains("TestFormData3"));
    }

    [Test]
    public void IgnoreFormData()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormData1", "FormDataValue");
      request.Form.Add("TestFormData2", "FormDataValue");
      request.Form.Add("TestFormData3", "FormDataValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "TestFormData2" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormData1"));
      Assert.IsTrue(message.Form.Contains("TestFormData3"));
    }

    [Test]
    public void IgnoreMultipleFormData()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormData1", "FormDataValue");
      request.Form.Add("TestFormData2", "FormDataValue");
      request.Form.Add("TestFormData3", "FormDataValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "TestFormData1", "TestFormData3" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(1, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormData2"));
    }

    [Test]
    public void IgnoreAllFormData()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormData1", "FormDataValue");
      request.Form.Add("TestFormData2", "FormDataValue");
      request.Form.Add("TestFormData3", "FormDataValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Form.Count);
    }

    private HttpRequest CreateWritableRequest()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      Type t = request.Form.GetType();
      PropertyInfo p = t.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
      p.SetValue(request.Form, false, null);
      return request;
    }

    // Cookies

    [Test]
    public void Cookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var message = new RaygunRequestMessage(request, new RaygunRequestMessageOptions());

      Assert.AreEqual(3, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookie1"));
      Assert.AreEqual(1, CookieCount(message, "TestCookie2"));
      Assert.AreEqual(1, CookieCount(message, "TestCookie3"));
    }

    [Test]
    public void DuplicateCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      Assert.AreEqual(2, request.Cookies.Count);

      var message = new RaygunRequestMessage(request, new RaygunRequestMessageOptions());

      Assert.AreEqual(2, message.Cookies.Count);
      Assert.AreEqual(2, CookieCount(message, "TestCookie"));
    }

    [Test]
    public void IgnoreCookie()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie2" }, Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookie1"));
      Assert.AreEqual(1, CookieCount(message, "TestCookie3"));
    }

    [Test]
    public void IgnoreDuplicateCookie()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie1" }, Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(1, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookie2"));
    }

    [Test]
    public void IgnoreMultipleCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie1", "TestCookie3" }, Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(1, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookie2"));
    }

    [Test]
    public void IgnoreAllCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*" }, Enumerable.Empty<string>());
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Cookies.Count);
    }

    private int CookieCount(RaygunRequestMessage message, string name)
    {
      int count = 0;
      foreach(Mindscape.Raygun4Net.Messages.RaygunRequestMessage.Cookie cookie in message.Cookies)
      {
        if (name.Equals(cookie.Name))
        {
          count++;
        }
      }
      return count;
    }
  }
}
