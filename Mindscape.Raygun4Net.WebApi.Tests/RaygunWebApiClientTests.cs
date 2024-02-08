using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.WebApi.Tests.Model;
using NUnit.Framework;
using System.Reflection;
using System.IO;

namespace Mindscape.Raygun4Net.WebApi.Tests
{
  [TestFixture]
  class RaygunWebApiClientTests
  {
    private FakeRaygunWebApiClient _client;
    private Exception _exception = new NullReferenceException("The thing is null");

    [SetUp]
    public void SetUp()
    {
      _client = new FakeRaygunWebApiClient();
    }

    [Test]
    public void CanNotSendIfExcludingStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsFalse(_client.ExposeCanSend(message));
    }

    [Test]
    public void CanNotSendIfExcludingStatusCode_MultipleCodes()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "400, 404, 501";

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsFalse(_client.ExposeCanSend(message));
    }

    [Test]
    public void CanSendIfNotExcludingStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "400";

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsTrue(_client.ExposeCanSend(message));
    }

    [Test]
    public void CanSendIfNotExcludingAnyStatusCodes()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = null;

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsTrue(_client.ExposeCanSend(message));
    }

    // this test is to make sure there is good null check coverage
    [Test]
    public void CanSendIfMessageIsNull()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = null;

      // Null message
      Assert.IsTrue(_client.ExposeCanSend(null));

      // Null message details
      Assert.IsTrue(_client.ExposeCanSend(new RaygunMessage()));

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails()
      };

      // Null message response

      Assert.IsTrue(_client.ExposeCanSend(message));
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

    [Test]
    public void StripReflectionTypeLoadException()
    {
      _client.AddWrapperExceptions(typeof(ReflectionTypeLoadException));

      FileNotFoundException ex1 = new FileNotFoundException();
      FileNotFoundException ex2 = new FileNotFoundException();
      ReflectionTypeLoadException wrapper = new ReflectionTypeLoadException(new Type[] { typeof(FakeRaygunWebApiClient), typeof(WrapperException) }, new Exception[] { ex1, ex2 });

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.Contains(ex1, exceptions);
      Assert.Contains(ex2, exceptions);

      Assert.IsTrue(ex1.Data["Type"].ToString().Contains("FakeRaygunWebApiClient"));
      Assert.IsTrue(ex2.Data["Type"].ToString().Contains("WrapperException"));
    }
  }
}
