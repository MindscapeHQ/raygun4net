using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public void DefaultUserInfo()
    {
      Assert.IsNull(_client.UserInfo);
    }

    [Test]
    public void UserProperty()
    {
      _client.User = "Robbie Robot";
      Assert.AreEqual("Robbie Robot", _client.User);
    }

    [Test]
    public void UserInfoProperty()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot");
      _client.UserInfo = user;
      Assert.AreSame(user, _client.UserInfo);
    }

    [Test]
    public void MessageWithNoUser()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.IsNull(message.Details.User);
    }

    [Test]
    public void MessageWithUser()
    {
      _client.User = "Robbie Robot";

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.AreEqual("Robbie Robot", message.Details.User.Identifier);
    }

    [Test]
    public void MessageWithUserInfo()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot") { IsAnonymous = true };
      _client.UserInfo = user;

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.AreEqual("Robbie Robot", message.Details.User.Identifier);
      Assert.IsTrue(message.Details.User.IsAnonymous);
    }

    [Test]
    public void UserInfoTrumpsUser()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot") { IsAnonymous = true };
      _client.UserInfo = user;
      _client.User = "Not Robbie Robot";

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.AreEqual("Robbie Robot", message.Details.User.Identifier);
      Assert.IsTrue(message.Details.User.IsAnonymous);
    }

    [Test]
    public void MessageWithUserInfoFromBuild()
    {
        RaygunMessage message = _client.ExposeBuildMessage(_exception, null, null, new RaygunIdentifierMessage("Robbie Robot"));
        Assert.AreEqual("Robbie Robot", message.Details.User.Identifier);
        Assert.IsFalse(message.Details.User.IsAnonymous);
    }

    [Test]
    public void UserInfoFromBuildTrumpsAll()
    {
        RaygunIdentifierMessage user = new RaygunIdentifierMessage("Not Robbie Robot") { IsAnonymous = true };
        _client.UserInfo = user;
        _client.User = "Also Not Robbie Robot";

        RaygunMessage message = _client.ExposeBuildMessage(_exception, null, null, new RaygunIdentifierMessage("Robbie Robot"));
        Assert.AreEqual("Robbie Robot", message.Details.User.Identifier);
        Assert.IsFalse(message.Details.User.IsAnonymous);
    }

    [Test]
    public void IsAnonymousDefault()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot");
      Assert.IsFalse(user.IsAnonymous);

      _client.User = "Robbie Robot";
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.IsFalse(message.Details.User.IsAnonymous);
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

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.AreEqual("Custom Version", message.Details.Version);
    }

    // Exception stripping tests

    [Test]
    public void StripTargetInvocationExceptionByDefault()
    {
      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      RaygunMessage message = _client.ExposeBuildMessage(wrapper);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void StripHttpUnhandledExceptionByDefault()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);

      RaygunMessage message = _client.ExposeBuildMessage(wrapper);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void StripSpecifiedWrapperException()
    {
      _client.AddWrapperExceptions(new Type[] { typeof(WrapperException) });

      WrapperException wrapper = new WrapperException(_exception);

      RaygunMessage message = _client.ExposeBuildMessage(wrapper);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void DontStripIfNoInnerException()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException();

      RaygunMessage message = _client.ExposeBuildMessage(wrapper);
      Assert.AreEqual("System.Web.HttpUnhandledException", message.Details.Error.ClassName);
      Assert.IsNull(message.Details.Error.InnerError);
    }

    [Test]
    public void DontStripNull()
    {
      RaygunMessage message = _client.ExposeBuildMessage(null);
      Assert.IsNull(message.Details.Error);
    }

    [Test]
    public void StripMultipleWrapperExceptions()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);
      TargetInvocationException wrapper2 = new TargetInvocationException(wrapper);

      RaygunMessage message = _client.ExposeBuildMessage(wrapper2);
      Assert.AreEqual("System.NullReferenceException", message.Details.Error.ClassName);
    }

    [Test]
    public void RemoveWrapperExceptions()
    {
      _client.RemoveWrapperExceptions(typeof(TargetInvocationException));

      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      RaygunMessage message = _client.ExposeBuildMessage(wrapper);
      Assert.AreEqual("System.Reflection.TargetInvocationException", message.Details.Error.ClassName);
    }

    // Validation tests

    [Test]
    public void NoAPIKeyIsInvalid()
    {
      Assert.IsFalse(_client.ExposeValidateApiKey());
    }

    [Test]
    public void APIKeyIsValid()
    {
      FakeRaygunClient client = new FakeRaygunClient("MY_API_KEY");
      Assert.IsTrue(client.ExposeValidateApiKey());
    }

    // Tags and user custom data tests

    [Test]
    public void TagsAreNullByDefault()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.IsNull(message.Details.Tags);
    }

    [Test]
    public void Tags()
    {
      IList<string> tags = new List<string>();
      tags.Add("Very Important");
      tags.Add("WPF");

      RaygunMessage message = _client.ExposeBuildMessage(_exception, tags);
      Assert.IsNotNull(message.Details.Tags);
      Assert.AreEqual(2, message.Details.Tags.Count);
      Assert.Contains("Very Important", (ICollection)message.Details.Tags);
      Assert.Contains("WPF", (ICollection)message.Details.Tags);
    }

    [Test]
    public void UserCustomDataIsNullByDefault()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.IsNull(message.Details.UserCustomData);
    }

    [Test]
    public void UserCustomData()
    {
      IDictionary data = new Dictionary<string, string>();
      data.Add("x", "42");
      data.Add("obj", "NULL");

      RaygunMessage message = _client.ExposeBuildMessage(_exception, null, data);
      Assert.IsNotNull(message.Details.UserCustomData);
      Assert.AreEqual(2, message.Details.UserCustomData.Count);
      Assert.AreEqual("42", message.Details.UserCustomData["x"]);
      Assert.AreEqual("NULL", message.Details.UserCustomData["obj"]);
    }

    // Cancel send tests

    [Test]
    public void NoHandlerSendsAll()
    {
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void HandlerIsChecked()
    {
      bool filterCalled = false;
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        Assert.AreEqual("NullReferenceException: The thing is null", e.Message.Details.Error.Message);
        filterCalled = true;
        e.Cancel = true;
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
      Assert.IsTrue(filterCalled);
    }

    [Test]
    public void HandlerCanAllowSend()
    {
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        // Allow send by not setting e.Cancel
      };
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void AllHandlersAreChecked()
    {
      bool filter1Called = false;
      bool filter2Called = false;
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        Assert.AreEqual("NullReferenceException: The thing is null", e.Message.Details.Error.Message);
        filter1Called = true;
        e.Cancel = true;
      };
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        Assert.AreEqual("NullReferenceException: The thing is null", e.Message.Details.Error.Message);
        filter2Called = true;
        e.Cancel = true;
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
      Assert.IsTrue(filter1Called);
      Assert.IsTrue(filter2Called);
    }

    [Test]
    public void DontSendIfFirstHandlerCancels()
    {
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        e.Cancel = true;
      };
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        // Allow send by not setting e.Cancel
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void DontSendIfSecondHandlerCancels()
    {
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        // Allow send by not setting e.Cancel
      };
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        e.Cancel = true;
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void AllowSendIfNoHandlerCancels()
    {
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        // Allow send by not setting e.Cancel
      };
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        // Allow send by not setting e.Cancel
      };
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void HandlerCanModifyMessage()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.AreEqual("NullReferenceException: The thing is null", message.Details.Error.Message);

      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        e.Message.Details.Error.Message = "Custom error message";
      };

      Assert.IsTrue(_client.ExposeOnSendingMessage(message));
      Assert.AreEqual("Custom error message", message.Details.Error.Message);
    }

    // CanSend tests

    [Test]
    public void CanSend()
    {
      Assert.IsTrue(_client.ExposeCanSend(_exception));
    }

    [Test]
    public void CannotSendSentException()
    {
      Exception exception = new InvalidOperationException("You cannot do that");
      _client.ExposeFlagAsSent(exception);
      Assert.IsFalse(_client.ExposeCanSend(exception));
    }

    [Test]
    public void CanSendIfExceptionIsUnknown()
    {
      Assert.IsTrue(_client.ExposeCanSend(null));
    }

    [Test]
    public void FlagNullAsSent()
    {
      Assert.DoesNotThrow(() => { _client.ExposeFlagAsSent(null); });
    }
  }
}