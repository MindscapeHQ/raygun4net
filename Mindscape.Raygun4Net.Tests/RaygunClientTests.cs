using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunClientTests
  {
    private FakeRaygunClient _client;
    private Exception _exception = new NullReferenceException("The thing is null");

    [SetUp]
    public void SetUp()
    {
      _client = new FakeRaygunClient();
    }

    // User tests

    [Test]
    public void DefaultUser()
    {
      Assert.IsNull(_client.User);
    }

    [Test]
    public void UserProperty()
    {
      _client.User = "Robbie Robot";
      Assert.AreEqual("Robbie Robot", _client.User);
    }

    [Test]
    public void MessageWithUser()
    {
      _client.User = "Robbie Robot";

      RaygunMessage message = _client.CreateMessage(_exception);
      Assert.AreEqual("Robbie Robot", message.Details.User.Identifier);
    }

    // Application version tests

    [Test]
    public void DefaultApplicationVersion()
    {
      Assert.IsNull(_client.ApplicationVersion);
    }

    [Test]
    public void ApplicationVersionProperty()
    {
      _client.ApplicationVersion = "Custom Version";
      Assert.AreEqual("Custom Version", _client.ApplicationVersion);
    }

    [Test]
    public void SetCustomApplicationVersion()
    {
      _client.ApplicationVersion = "Custom Version";

      RaygunMessage message = _client.CreateMessage(_exception);
      Assert.AreEqual("Custom Version", message.Details.Version);
    }

    // Exception stripping tests

    [Test]
    public void StripTargetInvocationExceptionByDefault()
    {
      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      RaygunMessage message = _client.CreateMessage(wrapper);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void StripHttpUnhandledExceptionByDefault()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);

      RaygunMessage message = _client.CreateMessage(wrapper);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void StripSpecifiedWrapperException()
    {
      _client.AddWrapperExceptions(new Type[] { typeof(WrapperException) });

      WrapperException wrapper = new WrapperException(_exception);

      RaygunMessage message = _client.CreateMessage(wrapper);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void DontStripIfNoInnerException()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException();

      RaygunMessage message = _client.CreateMessage(wrapper);
      Assert.AreEqual("System.Web.HttpUnhandledException", message.Details.Error.ClassName);
      Assert.IsNull(message.Details.Error.InnerError);
    }

    [Test]
    public void StripMultipleWrapperExceptions()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);
      TargetInvocationException wrapper2 = new TargetInvocationException(wrapper);

      RaygunMessage message = _client.CreateMessage(wrapper2);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    // Validation tests

    [Test]
    public void NoAPIKeyIsInvalid()
    {
      Assert.IsFalse(_client.Validate());
    }

    [Test]
    public void APIKeyIsValid()
    {
      FakeRaygunClient client = new FakeRaygunClient("MY_API_KEY");
      Assert.IsTrue(client.Validate());
    }

    // Tags and user custom data tests

    [Test]
    public void TagsAreNullByDefault()
    {
      RaygunMessage message = _client.CreateMessage(_exception);
      Assert.IsNull(message.Details.Tags);
    }

    [Test]
    public void Tags()
    {
      IList<string> tags = new List<string>();
      tags.Add("Very Important");
      tags.Add("WPF");

      RaygunMessage message = _client.CreateMessage(_exception, tags);
      Assert.IsNotNull(message.Details.Tags);
      Assert.AreEqual(2, message.Details.Tags.Count);
      Assert.Contains("Very Important", (ICollection)message.Details.Tags);
      Assert.Contains("WPF", (ICollection)message.Details.Tags);
    }

    [Test]
    public void UserCustomDataIsNullByDefault()
    {
      RaygunMessage message = _client.CreateMessage(_exception);
      Assert.IsNull(message.Details.UserCustomData);
    }

    [Test]
    public void UserCustomData()
    {
      IDictionary data = new Dictionary<string, string>();
      data.Add("x", "42");
      data.Add("obj", "NULL");

      RaygunMessage message = _client.CreateMessage(_exception, null, data);
      Assert.IsNotNull(message.Details.UserCustomData);
      Assert.AreEqual(2, message.Details.UserCustomData.Count);
      Assert.AreEqual("42", message.Details.UserCustomData["x"]);
      Assert.AreEqual("NULL", message.Details.UserCustomData["obj"]);
    }
  }
}