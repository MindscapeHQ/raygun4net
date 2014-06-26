using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net2.Tests
{
  [TestFixture]
  public class RaygunRequestMessageTests
  {
    private HttpRequest _defaultRequest;
    private List<string> _empty = new List<string>();

    [SetUp]
    public void SetUp()
    {
      _defaultRequest = new HttpRequest(string.Empty, "http://google.com", string.Empty);
    }

    [Test]
    public void HostNameTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, null);

      Assert.That(message.HostName, Is.EqualTo("google.com"));
    }

    [Test]
    public void UrlTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, null);

      Assert.That(message.Url, Is.EqualTo("/"));
    }

    [Test]
    public void HttpMethodTest()
    {
      var message = new RaygunRequestMessage(_defaultRequest, null);

      Assert.That(message.HttpMethod, Is.EqualTo("GET"));
    }

    [Test]
    public void QueryStringTest()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");

      var message = new RaygunRequestMessage(request, null);

      Assert.That(message.QueryString, Contains.Item(new KeyValuePair<string, string>("test", "test")));
    }

    // Form fields

    [Test]
    public void FormFields()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var message = new RaygunRequestMessage(request, new RaygunRequestMessageOptions());

      Assert.AreEqual(3, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormField1"));
      Assert.IsTrue(message.Form.Contains("TestFormField2"));
      Assert.IsTrue(message.Form.Contains("TestFormField3"));
    }

    [Test]
    public void IgnoreFormFields()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "TestFormField2" }, _empty, _empty, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormField1"));
      Assert.IsTrue(message.Form.Contains("TestFormField3"));
    }

    [Test]
    public void IgnoreMultipleFormFields()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "TestFormField1", "TestFormField3" }, _empty, _empty, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(1, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormField2"));
    }

    [Test]
    public void IgnoreAllFormFields()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "*" }, _empty, _empty, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Form.Count);
    }

    [Test]
    public void IgnoreFormField_StartsWith()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormFieldTest", "FormFieldValue");
      request.Form.Add("TestFormField", "FormFieldValue");
      request.Form.Add("FormFieldTest", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "formfield*" }, _empty, _empty, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormFieldTest"));
      Assert.IsTrue(message.Form.Contains("TestFormField"));
    }

    [Test]
    public void IgnoreFormField_EndsWith()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormFieldTest", "FormFieldValue");
      request.Form.Add("TestFormField", "FormFieldValue");
      request.Form.Add("FormFieldTest", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "*formfield" }, _empty, _empty, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormFieldTest"));
      Assert.IsTrue(message.Form.Contains("FormFieldTest"));
    }

    [Test]
    public void IgnoreFormField_Contains()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormFieldTest", "FormFieldValue");
      request.Form.Add("TestFormField", "FormFieldValue");
      request.Form.Add("FormFieldTest", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "*formfield*" }, _empty, _empty, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Form.Count);
    }

    [Test]
    public void IgnoreFormField_CaseInsensitive()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TESTFORMFIELD", "FormFieldValue");
      Assert.AreEqual(1, request.Form.Count);

      var options = new RaygunRequestMessageOptions(new string[] { "testformfield" }, _empty, _empty, _empty);
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

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "TestCookie2" }, _empty);
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

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "TestCookie1" }, _empty);
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

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "TestCookie1", "TestCookie3" }, _empty);
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

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "*" }, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Cookies.Count);
    }

    [Test]
    public void IgnoreCookie_StartsWith()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookieTest", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("CookieTest", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "cookie*" }, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookieTest"));
      Assert.AreEqual(1, CookieCount(message, "TestCookie"));
    }

    [Test]
    public void IgnoreCookie_EndsWith()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookieTest", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("CookieTest", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "*cookie" }, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(2, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookieTest"));
      Assert.AreEqual(1, CookieCount(message, "CookieTest"));
    }

    [Test]
    public void IgnoreCookie_Contains()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookieTest", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("CookieTest", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "*cookie*" }, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Cookies.Count);
    }

    [Test]
    public void IgnoreCookie_CaseInsensitive()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TESTCOOKIE", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("testcookie", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(_empty, _empty, new string[] { "TeStCoOkIe" }, _empty);
      var message = new RaygunRequestMessage(request, options);

      Assert.AreEqual(0, message.Cookies.Count);
    }

    private int CookieCount(RaygunRequestMessage message, string name)
    {
      int count = 0;
      foreach (Mindscape.Raygun4Net.Messages.RaygunRequestMessage.Cookie cookie in message.Cookies)
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
