using System.Linq;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

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
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
    }

    [Test]
    public void HandlerIsChecked()
    {
      bool filterCalled = false;
      _client.SendingMessage += (o, e) =>
      {
        Assert.AreEqual("The thing is null", e.Message.Details.Error.Message);
        filterCalled = true;
        e.Cancel = true;
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
      Assert.IsTrue(filterCalled);
    }

    [Test]
    public void HandlerCanAllowSend()
    {
      _client.SendingMessage += (o, e) =>
      {
        // Allow send by not setting e.Cancel
      };
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
    }

    [Test]
    public void AllHandlersAreChecked()
    {
      bool filter1Called = false;
      bool filter2Called = false;
      _client.SendingMessage += (o, e) =>
      {
        Assert.AreEqual("The thing is null", e.Message.Details.Error.Message);
        filter1Called = true;
        e.Cancel = true;
      };
      _client.SendingMessage += (o, e) =>
      {
        Assert.AreEqual("The thing is null", e.Message.Details.Error.Message);
        filter2Called = true;
        e.Cancel = true;
      };
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
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
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
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
      Assert.IsFalse(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
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
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception), _exception));
    }

    [Test]
    public void HandlerCanModifyMessage()
    {
      RaygunMessage message = _client.ExposeBuildMessage(_exception);
      Assert.AreEqual("The thing is null", message.Details.Error.Message);

      _client.SendingMessage += (o, e) =>
      {
        e.Message.Details.Error.Message = "Custom error message";
      };

      Assert.IsTrue(_client.ExposeOnSendingMessage(message, _exception));
      Assert.AreEqual("Custom error message", message.Details.Error.Message);
    }

    // Exception stripping tests

    [Test]
    public void StripTargetInvocationExceptionByDefault()
    {
      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.Contains(_exception, exceptions);
    }

    [Test]
    public void StripSpecifiedWrapperException()
    {
      _client.AddWrapperExceptions(typeof(WrapperException));

      WrapperException wrapper = new WrapperException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.Contains(_exception, exceptions);
    }

    [Test]
    public void DontStripIfNoInnerException()
    {
      TargetInvocationException wrapper = new TargetInvocationException(null);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.Contains(wrapper, exceptions);
    }

    [Test]
    public void DontStripNull()
    {
      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(null).ToList();
      Assert.AreEqual(1, exceptions.Count); // The current expected behaviour is that you can pass null to the Send methods and cause Raygun to send a report.
      Assert.Contains(null, exceptions);
    }

    [Test]
    public void StripMultipleWrapperExceptions()
    {
      _client.AddWrapperExceptions(typeof(WrapperException));

      WrapperException wrapper = new WrapperException(_exception);
      TargetInvocationException wrapper2 = new TargetInvocationException(wrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper2).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.Contains(_exception, exceptions);
    }

    [Test]
    public void RemoveWrapperExceptions()
    {
      _client.RemoveWrapperExceptions(typeof(TargetInvocationException));

      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.Contains(wrapper, exceptions);
    }

    [Test]
    public void StripAggregateException()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      AggregateException wrapper = new AggregateException(_exception, exception2);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.Contains(_exception, exceptions);
      Assert.Contains(exception2, exceptions);
    }

    [Test]
    public void StripAggregateExceptionAndTargetInvocationException()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      TargetInvocationException innerWrapper = new TargetInvocationException(exception2);
      AggregateException wrapper = new AggregateException(_exception, innerWrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.Contains(_exception, exceptions);
      Assert.Contains(exception2, exceptions);
    }

    [Test]
    public void StripTargetInvocationExceptionAndAggregateException()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      AggregateException innerWrapper = new AggregateException(_exception, exception2);
      TargetInvocationException wrapper = new TargetInvocationException(innerWrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.Contains(_exception, exceptions);
      Assert.Contains(exception2, exceptions);
    }

    [Test]
    public void StripNestedAggregateExceptions()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      NotSupportedException exception3 = new NotSupportedException("Forgot to implement this method");
      AggregateException innerWrapper = new AggregateException(_exception, exception2);
      AggregateException wrapper = new AggregateException(innerWrapper, exception3);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(3, exceptions.Count);
      Assert.Contains(_exception, exceptions);
      Assert.Contains(exception2, exceptions);
      Assert.Contains(exception3, exceptions);
    }
  }
}
