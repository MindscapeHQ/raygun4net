using System;
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
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var message = RaygunRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

      Assert.That(3, Is.EqualTo(message.Form.Count));
      Assert.That(message.Form.Contains("TestFormField1"), Is.True);
      Assert.That(message.Form.Contains("TestFormField2"), Is.True);
      Assert.That(message.Form.Contains("TestFormField3"), Is.True);
    }

    [Test]
    public void IgnoreFormField()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestFormField2" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(2, Is.EqualTo(message.Form.Count));
      Assert.That(message.Form.Contains("TestFormField1"), Is.True);
      Assert.That(message.Form.Contains("TestFormField3"), Is.True);
    }

    [Test]
    public void IgnoreMultipleFormFields()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestFormField1", "TestFormField3" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(1, Is.EqualTo(message.Form.Count));
      Assert.That(message.Form.Contains("TestFormField2"), Is.True);
    }

    [Test]
    public void IgnoreAllFormFields()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormField1", "FormFieldValue");
      request.Form.Add("TestFormField2", "FormFieldValue");
      request.Form.Add("TestFormField3", "FormFieldValue");
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(0, Is.EqualTo(message.Form.Count));
    }

    [Test]
    public void IgnoreFormField_StartsWith()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormFieldTest", "FormFieldValue");
      request.Form.Add("TestFormField", "FormFieldValue");
      request.Form.Add("FormFieldTest", "FormFieldValue");
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "formfield*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(2, Is.EqualTo(message.Form.Count));
      Assert.That(message.Form.Contains("TestFormFieldTest"), Is.True);
      Assert.That(message.Form.Contains("TestFormField"), Is.True);
    }

    [Test]
    public void IgnoreFormField_EndsWith()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormFieldTest", "FormFieldValue");
      request.Form.Add("TestFormField", "FormFieldValue");
      request.Form.Add("FormFieldTest", "FormFieldValue");
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*formfield" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(2, Is.EqualTo(message.Form.Count));
      Assert.That(message.Form.Contains("TestFormFieldTest"), Is.True);
      Assert.That(message.Form.Contains("FormFieldTest"), Is.True);
    }

    [Test]
    public void IgnoreFormField_Contains()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TestFormFieldTest", "FormFieldValue");
      request.Form.Add("TestFormField", "FormFieldValue");
      request.Form.Add("FormFieldTest", "FormFieldValue");
      Assert.That(3, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*formfield*" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(0, Is.EqualTo(message.Form.Count));
    }

    [Test]
    public void IgnoreFormField_CaseInsensitive()
    {
      var request = CreateWritableRequest();

      request.Form.Add("TESTFORMFIELD", "FormFieldValue");
      Assert.That(1, Is.EqualTo(request.Form.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "testformfield" }, Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(0, Is.EqualTo(message.Form.Count));
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
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var message = RaygunRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

      Assert.That(3, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookie1"), Is.EqualTo(1));
      Assert.That(CookieCount(message, "TestCookie2"), Is.EqualTo(1));
      Assert.That(CookieCount(message, "TestCookie3"), Is.EqualTo(1));
    }

    [Test]
    public void DuplicateCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      Assert.That(2, Is.EqualTo(request.Cookies.Count));

      var message = RaygunRequestMessageBuilder.Build(request, new RaygunRequestMessageOptions());

      Assert.That(2, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookie"), Is.EqualTo(2));
    }

    [Test]
    public void IgnoreCookie()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie2" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(2, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookie1"), Is.EqualTo(1));
      Assert.That(CookieCount(message, "TestCookie3"), Is.EqualTo(1));
    }

    [Test]
    public void IgnoreDuplicateCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie1" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(1, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookie2"), Is.EqualTo(1));
    }

    [Test]
    public void IgnoreMultipleCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TestCookie1", "TestCookie3" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(1, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookie2"), Is.EqualTo(1));
    }

    [Test]
    public void IgnoreAllCookies()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookie1", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie2", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie3", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(0, Is.EqualTo(message.Cookies.Count));
    }

    [Test]
    public void IgnoreCookie_StartsWith()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookieTest", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("CookieTest", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "cookie*" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(2, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookieTest"), Is.EqualTo(1));
      Assert.That(CookieCount(message, "TestCookie"), Is.EqualTo(1));
    }

    [Test]
    public void IgnoreCookie_EndsWith()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookieTest", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("CookieTest", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*cookie" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(2, Is.EqualTo(message.Cookies.Count));
      Assert.That(CookieCount(message, "TestCookieTest"), Is.EqualTo(1));
      Assert.That(CookieCount(message, "CookieTest"), Is.EqualTo(1));
    }

    [Test]
    public void IgnoreCookie_Contains()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TestCookieTest", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("CookieTest", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "*cookie*" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(0, Is.EqualTo(message.Cookies.Count));
    }

    [Test]
    public void IgnoreCookie_CaseInsensitive()
    {
      var request = new HttpRequest("test", "http://google.com", "test=test");
      request.Cookies.Add(new HttpCookie("TESTCOOKIE", "CookieValue"));
      request.Cookies.Add(new HttpCookie("TestCookie", "CookieValue"));
      request.Cookies.Add(new HttpCookie("testcookie", "CookieValue"));
      Assert.That(3, Is.EqualTo(request.Cookies.Count));

      var options = new RaygunRequestMessageOptions(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), new string[] { "TeStCoOkIe" }, Enumerable.Empty<string>());
      var message = RaygunRequestMessageBuilder.Build(request, options);

      Assert.That(0, Is.EqualTo(message.Cookies.Count));
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

      Assert.That(1, Is.EqualTo(ignored.Count));
      Assert.That("Password", Is.EqualTo(ignored.Keys.First()));
      Assert.That("p", Is.EqualTo(ignored["Password"]));
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

      Assert.That(2, Is.EqualTo(ignored.Count));
      Assert.That(ignored.Keys.Contains("Password"), Is.True);
      Assert.That("p", Is.EqualTo(ignored["Password"]));
      Assert.That(ignored.Keys.Contains("SensitiveNumber"), Is.True);
      Assert.That("7", Is.EqualTo(ignored["SensitiveNumber"]));
    }

    [Test]
    public void StripIgnoredFormData()
    {
      string rawData = "------WebKitFormBoundarye64VBkpu4PoxFbpl Content-Disposition: form-data; name=\"Password\"\r\n\r\nsecret ------WebKitFormBoundarye64VBkpu4PoxFbpl--";
      Dictionary<string, string> ignored = new Dictionary<string, string>() { { "Password", "secret" } };
      rawData = FakeRaygunRequestMessageBuilder.ExposeStripIgnoredFormData(rawData, ignored);

      Assert.That("------WebKitFormBoundarye64VBkpu4PoxFbpl Content-Disposition: form-data;  ------WebKitFormBoundarye64VBkpu4PoxFbpl--", Is.EqualTo(rawData));
    }
  }
}
