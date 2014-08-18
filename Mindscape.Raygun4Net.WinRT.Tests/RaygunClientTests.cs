using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;
using System;

namespace Mindscape.Raygun4Net.WinRT.Tests
{
  [TestFixture]
  public class RaygunClientTests
  {
    private FakeRaygunClient _client;
    private readonly Exception _exception = new NullReferenceException("The thing is null");

    [SetUp]
    public void SetUp()
    {
      _client = new FakeRaygunClient();
    }

    [Test]
    public void NoHandlerSendsAll()
    {
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void HandlerIsChecked()
    {
      bool filterCalled = false;
      _client.SendingMessage += (o, e) =>
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
      _client.SendingMessage += (o, e) =>
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
      _client.SendingMessage += (o, e) =>
      {
        Assert.AreEqual("NullReferenceException: The thing is null", e.Message.Details.Error.Message);
        filter1Called = true;
        e.Cancel = true;
      };
      _client.SendingMessage += (o, e) =>
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
      _client.SendingMessage += (o, e) =>
      {
        e.Cancel = true;
      };
      _client.SendingMessage += (o, e) =>
      {
        // Allow send by not setting e.Cancel
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void DontSendIfSecondHandlerCancels()
    {
      _client.SendingMessage += (o, e) =>
      {
        // Allow send by not setting e.Cancel
      };
      _client.SendingMessage += (o, e) =>
      {
        e.Cancel = true;
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }

    [Test]
    public void AllowSendIfNoHandlerCancels()
    {
      _client.SendingMessage += (o, e) =>
      {
        // Allow send by not setting e.Cancel
      };
      _client.SendingMessage += (o, e) =>
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

      _client.SendingMessage += (o, e) =>
      {
        e.Message.Details.Error.Message = "Custom error message";
      };

      Assert.IsTrue(_client.ExposeOnSendingMessage(message));
      Assert.AreEqual("Custom error message", message.Details.Error.Message);
    }
  }
}
