﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Web;
using Mindscape.Raygun4Net.Builders;
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
      var message = RaygunRequestMessageBuilder.Build(_defaultRequest, null);

      Assert.That(message.HostName, Is.EqualTo("google.com"));
    }

    [Test]
    public void UrlTest()
    {
      var message = RaygunRequestMessageBuilder.Build(_defaultRequest, null);

      Assert.That(message.Url, Is.EqualTo("/"));
    }

    [Test]
    public void HttpMethodTest()
    {
      var message = RaygunRequestMessageBuilder.Build(_defaultRequest, null);

      Assert.That(message.HttpMethod, Is.EqualTo("GET"));
    }

    [Test]
    public void QueryStringTest()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");

      var message = RaygunRequestMessageBuilder.Build(request, null);

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

      var message = RaygunRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

      Assert.AreEqual(3, message.Form.Count);
      Assert.IsTrue(message.Form.Contains("TestFormField1"));
      Assert.IsTrue(message.Form.Contains("TestFormField2"));
      Assert.IsTrue(message.Form.Contains("TestFormField3"));
    }

    [Test]
    public void IgnoreFormField()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.AreEqual(3, request.Form.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestFormField2" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestFormField1", "TestFormField3" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "formfield*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*formfield" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*formfield*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.AreEqual(0, message.Form.Count);
    }

    [Test]
    public void IgnoreFormField_CaseInsensitive()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TESTFORMFIELD", "FormFieldValue");
      Assert.AreEqual(1, request.Form.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "testformfield" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var message = RaygunRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

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

      var message = RaygunRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie2" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.AreEqual(2, message.Cookies.Count);
      Assert.AreEqual(1, CookieCount(message, "TestCookie1"));
      Assert.AreEqual(1, CookieCount(message, "TestCookie3"));
    }

    [Test]
    public void IgnoreDuplicateCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      Assert.AreEqual(3, request.Cookies.Count);

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie1" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie1", "TestCookie3" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "cookie*" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*cookie" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*cookie*" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TeStCoOkIe" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

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

    // Helper method tests

    [Test]
    public void GetIgnoredFormValues()
    {
      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "Password" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      NameValueCollection form = new NameValueCollection();
      form.Add("Key", "Value");
      form.Add("Password", "p");

      Dictionary<string, string> ignored = FakeRaygunRequestMessageBuilder.ExposeGetIgnoredFormValues(form, options.IsFormFieldIgnored);

      Assert.AreEqual(1, ignored.Count);
      Assert.AreEqual("Password", ignored.Keys.First());
      Assert.AreEqual("p", ignored["Password"]);
    }

    [Test]
    public void GetIgnoredFormValues_MultipleIgnores()
    {
      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "Password", "SensitiveNumber" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      NameValueCollection form = new NameValueCollection();
      form.Add("SensitiveNumber", "7");
      form.Add("Key", "Value");
      form.Add("Password", "p");

      Dictionary<string, string> ignored = FakeRaygunRequestMessageBuilder.ExposeGetIgnoredFormValues(form, options.IsFormFieldIgnored);

      Assert.AreEqual(2, ignored.Count);
      Assert.IsTrue(ignored.Keys.Contains("Password"));
      Assert.AreEqual("p", ignored["Password"]);
      Assert.IsTrue(ignored.Keys.Contains("SensitiveNumber"));
      Assert.AreEqual("7", ignored["SensitiveNumber"]);
    }

    [Test]
    public void StripIgnoredFormData()
    {
      string rawData = "------WebKitFormBoundarye64VBkpu4PoxFbpl Content-Disposition: form-data; name=\"Password\"\r\n\r\nsecret ------WebKitFormBoundarye64VBkpu4PoxFbpl--";
      Dictionary<string, string> ignored = new Dictionary<string, string>() { { "Password", "secret" } };
      rawData = FakeRaygunRequestMessageBuilder.ExposeStripIgnoredFormData(rawData, ignored);

      Assert.AreEqual("------WebKitFormBoundarye64VBkpu4PoxFbpl Content-Disposition: form-data;  ------WebKitFormBoundarye64VBkpu4PoxFbpl--", rawData);
    }
  }
}
