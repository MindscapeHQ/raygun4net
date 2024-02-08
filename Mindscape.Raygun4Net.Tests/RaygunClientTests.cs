using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

    [Test]
    public void DefaultProxyCredentials()
    {
      Assert.That(_client.ProxyCredentials, Is.Null);
    }

    [Test]
    public void DefaultWebProxy()
    {
      Assert.That(_client.WebProxy, Is.Null);
    }

    [Test]
    public void ProxyCredentials()
    {
      var credentials = new NetworkCredential();
      _client.ProxyCredentials = credentials;
      Assert.That(credentials, Is.SameAs(_client.ProxyCredentials));
    }

    [Test]
    public void WebProxy()
    {
      var proxy = new WebProxy();
      _client.WebProxy = proxy;
      Assert.That(proxy, Is.SameAs(_client.WebProxy));
    }

    // User tests

    [Test]
    public void DefaultUser()
    {
      Assert.That(_client.User, Is.Null);
    }

    [Test]
    public void DefaultUserInfo()
    {
      Assert.That(_client.UserInfo, Is.Null);
    }

    [Test]
    public void UserProperty()
    {
      _client.User = "Robbie Robot";
      Assert.That("Robbie Robot", Is.EqualTo(_client.User));
    }

    [Test]
    public void UserInfoProperty()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot");
      _client.UserInfo = user;
      Assert.That(user, Is.SameAs(_client.UserInfo));
    }

    [Test]
    public void MessageWithNoUser()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That(message.Details.User, Is.Null);
    }

    [Test]
    public void MessageWithUser()
    {
      _client.User = "Robbie Robot";

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That("Robbie Robot", Is.EqualTo(message.Details.User.Identifier));
    }

    [Test]
    public void MessageWithUserInfo()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot") { IsAnonymous = true };
      _client.UserInfo = user;

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That("Robbie Robot", Is.EqualTo(message.Details.User.Identifier));
      Assert.That(message.Details.User.IsAnonymous, Is.True);
    }

    [Test]
    public void UserInfoTrumpsUser()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot") { IsAnonymous = true };
      _client.UserInfo = user;
      _client.User = "Not Robbie Robot";

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That("Robbie Robot", Is.EqualTo(message.Details.User.Identifier));
      Assert.That(message.Details.User.IsAnonymous, Is.True);
    }

    [Test]
    public void MessageWithUserInfoFromBuild()
    {
        RaygunMessage message = _client.ExposeBuildMessage(_exception, null, null, new RaygunIdentifierMessage("Robbie Robot"));
        Assert.That("Robbie Robot", Is.EqualTo(message.Details.User.Identifier));
        Assert.That(message.Details.User.IsAnonymous, Is.False);
    }

    [Test]
    public void UserInfoFromBuildTrumpsAll()
    {
        RaygunIdentifierMessage user = new RaygunIdentifierMessage("Not Robbie Robot") { IsAnonymous = true };
        _client.UserInfo = user;
        _client.User = "Also Not Robbie Robot";

        RaygunMessage message = _client.ExposeBuildMessage(_exception, null, null, new RaygunIdentifierMessage("Robbie Robot"));
        Assert.That("Robbie Robot", Is.EqualTo(message.Details.User.Identifier));
        Assert.That(message.Details.User.IsAnonymous, Is.False);
    }

    [Test]
    public void IsAnonymousDefault()
    {
      RaygunIdentifierMessage user = new RaygunIdentifierMessage("Robbie Robot");
      Assert.That(user.IsAnonymous, Is.False);

      _client.User = "Robbie Robot";
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That(message.Details.User.IsAnonymous, Is.False);
    }

    // Application version tests

    [Test]
    public void DefaultApplicationVersion()
    {
      Assert.That(_client.ApplicationVersion, Is.Null);
    }

    [Test]
    public void ApplicationVersionProperty()
    {
      _client.ApplicationVersion = "Custom Version";
      Assert.That("Custom Version", Is.EqualTo(_client.ApplicationVersion));
    }

    [Test]
    public void SetCustomApplicationVersion()
    {
      _client.ApplicationVersion = "Custom Version";

      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That("Custom Version", Is.EqualTo(message.Details.Version));
    }

    // Exception stripping tests

    [Test]
    public void StripTargetInvocationExceptionByDefault()
    {
      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      var exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      
      Assert.That(exceptions, Does.Not.Contain(wrapper));
      Assert.That(exceptions, Has.Exactly(1).TypeOf<NullReferenceException>());
    }

    [Test]
    public void StripHttpUnhandledExceptionByDefault()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);

      var exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      
      Assert.That(exceptions, Does.Not.Contain(wrapper));
      Assert.That(exceptions, Has.Exactly(1).TypeOf<NullReferenceException>());
    }

    [Test]
    public void StripSpecifiedWrapperException()
    {
      _client.AddWrapperExceptions(new Type[] { typeof(WrapperException) });

      WrapperException wrapper = new WrapperException(_exception);

      var exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      
      Assert.That(exceptions, Does.Not.Contain(wrapper));
      Assert.That(exceptions, Has.Exactly(1).TypeOf<NullReferenceException>());
    }

    [Test]
    public void DontStripIfNoInnerException()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException();

      var exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      
      Assert.That(exceptions, Does.Contain(wrapper));
      Assert.That(exceptions, Has.Exactly(1).TypeOf<HttpUnhandledException>());
    }

    [Test]
    public void DontStripNull()
    {
      RaygunMessage message = _client.ExposeBuildMessage(null);
      Assert.That(message.Details.Error, Is.Null);
    }

    [Test]
    public void StripMultipleWrapperExceptions()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);
      TargetInvocationException wrapper2 = new TargetInvocationException(wrapper);

      var exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      
      Assert.That(exceptions, Does.Not.Contain(wrapper));
      Assert.That(exceptions, Does.Not.Contain(wrapper2));
      Assert.That(exceptions, Has.Exactly(1).TypeOf<NullReferenceException>());
    }

    [Test]
    public void RemoveWrapperExceptions()
    {
      _client.RemoveWrapperExceptions(typeof(TargetInvocationException));

      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      RaygunMessage message = _client.ExposeBuildMessage(wrapper);
      Assert.That("System.Reflection.TargetInvocationException", Is.EqualTo(message.Details.Error.ClassName));
    }

    // Validation tests

    [Test]
    public void NoAPIKeyIsInvalid()
    {
      Assert.That(_client.ExposeValidateApiKey(), Is.False);
    }

    [Test]
    public void APIKeyIsValid()
    {
      FakeRaygunClient client = new FakeRaygunClient("MY_API_KEY");
      Assert.That(client.ExposeValidateApiKey(), Is.True);
    }

    // Tags and user custom data tests

    [Test]
    public void TagsAreNullByDefault()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That(message.Details.Tags, Is.Null);
    }

    [Test]
    public void Tags()
    {
      IList<string> tags = new List<string>();
      tags.Add("Very Important");
      tags.Add("WPF");

      RaygunMessage message = _client.ExposeBuildMessage(_exception, tags);
      Assert.That(message.Details.Tags, Is.Not.Null);
      Assert.That(2, Is.EqualTo(message.Details.Tags.Count));
      Assert.That((ICollection)message.Details.Tags, Contains.Item("Very Important"));
      Assert.That((ICollection)message.Details.Tags, Contains.Item("WPF"));
    }

    [Test]
    public void UserCustomDataIsNullByDefault()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That(message.Details.UserCustomData, Is.Null);
    }

    [Test]
    public void UserCustomData()
    {
      IDictionary data = new Dictionary<string, string>();
      data.Add("x", "42");
      data.Add("obj", "NULL");

      RaygunMessage message = _client.ExposeBuildMessage(_exception, null, data);
      Assert.That(message.Details.UserCustomData, Is.Not.Null);
      Assert.That(2, Is.EqualTo(message.Details.UserCustomData.Count));
      Assert.That("42", Is.EqualTo(message.Details.UserCustomData["x"]));
      Assert.That("NULL", Is.EqualTo(message.Details.UserCustomData["obj"]));
    }

    // Cancel send tests

    [Test]
    public void NoHandlerSendsAll()
    {
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.True);
    }

    [Test]
    public void HandlerIsChecked()
    {
      bool filterCalled = false;
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        Assert.That("The thing is null", Is.EqualTo(e.Message.Details.Error.Message));
        filterCalled = true;
        e.Cancel = true;
      };
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.False);
      Assert.That(filterCalled, Is.True);
    }

    [Test]
    public void HandlerCanAllowSend()
    {
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        // Allow send by not setting e.Cancel
      };
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.True);
    }

    [Test]
    public void AllHandlersAreChecked()
    {
      bool filter1Called = false;
      bool filter2Called = false;
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        Assert.That("The thing is null", Is.EqualTo(e.Message.Details.Error.Message));
        filter1Called = true;
        e.Cancel = true;
      };
      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        Assert.That("The thing is null", Is.EqualTo(e.Message.Details.Error.Message));
        filter2Called = true;
        e.Cancel = true;
      };
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.False);
      Assert.That(filter1Called, Is.True);
      Assert.That(filter2Called, Is.True);
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
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.False);
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
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.False);
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
      Assert.That(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)), Is.True);
    }

    [Test]
    public void HandlerCanModifyMessage()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.That("The thing is null", Is.EqualTo(message.Details.Error.Message));

      _client.SendingMessage += (object o, RaygunSendingMessageEventArgs e) =>
      {
        e.Message.Details.Error.Message = "Custom error message";
      };

      Assert.That(_client.ExposeOnSendingMessage(message), Is.True);
      Assert.That("Custom error message", Is.EqualTo(message.Details.Error.Message));
    }

    // CanSend tests

    [Test]
    public void CanSend()
    {
      Assert.That(_client.ExposeCanSend(_exception), Is.True);
    }

    [Test]
    public void CannotSendSentException()
    {
      Exception exception = new InvalidOperationException("You cannot do that");
      _client.ExposeFlagAsSent(exception);
      Assert.That(_client.ExposeCanSend(exception), Is.False);
    }

    [Test]
    public void CanSendIfExceptionIsUnknown()
    {
      Assert.That(_client.ExposeCanSend(null), Is.True);
    }

    [Test]
    public void FlagNullAsSent()
    {
      Assert.DoesNotThrow(() => { _client.ExposeFlagAsSent(null); });
    }

    // WebProxy creation tests
    [Test]
    public void WebProxyPropertyPreferredOverDefaultWebProxy()
    {
      var theProxyWeDontWant = new WebProxy();
      var theProxyWeDoWant = new WebProxy();
    
      WebRequest.DefaultWebProxy = theProxyWeDontWant;
      _client.WebProxy = theProxyWeDoWant;
      
      Assert.That(theProxyWeDoWant, Is.SameAs(WebClientHelper.WebProxy));
    }
  }
}