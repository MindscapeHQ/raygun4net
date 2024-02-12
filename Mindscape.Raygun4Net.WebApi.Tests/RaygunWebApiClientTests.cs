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

      Assert.That(_client.ExposeCanSend(message), Is.False);
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

      Assert.That(_client.ExposeCanSend(message), Is.False);
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

      Assert.That(_client.ExposeCanSend(message), Is.True);
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

      Assert.That(_client.ExposeCanSend(message), Is.True);
    }

    // this test is to make sure there is good null check coverage
    [Test]
    public void CanSendIfMessageIsNull()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = null;

      // Null message
      Assert.That(_client.ExposeCanSend(null), Is.True);

      // Null message details
      Assert.That(_client.ExposeCanSend(new RaygunMessage()), Is.True);

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails()
      };

      // Null message response

      Assert.That(_client.ExposeCanSend(message), Is.True);
    }

    // Exception stripping tests

    [Test]
    public void StripTargetInvocationExceptionByDefault()
    {
      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(1, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
    }

    [Test]
    public void StripSpecifiedWrapperException()
    {
      _client.AddWrapperExceptions(typeof(WrapperException));

      WrapperException wrapper = new WrapperException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(1, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
    }

    [Test]
    public void DontStripIfNoInnerException()
    {
      TargetInvocationException wrapper = new TargetInvocationException(null);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(1, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(wrapper));
    }

    [Test]
    public void DontStripNull()
    {
      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(null).ToList();
      Assert.That(1, Is.EqualTo(exceptions.Count)); // The current expected behaviour is that you can pass null to the Send methods and cause Raygun to send a report.
      Assert.That(exceptions, Contains.Item(null));
    }

    [Test]
    public void StripMultipleWrapperExceptions()
    {
      _client.AddWrapperExceptions(typeof(WrapperException));

      WrapperException wrapper = new WrapperException(_exception);
      TargetInvocationException wrapper2 = new TargetInvocationException(wrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper2).ToList();
      Assert.That(1, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
    }

    [Test]
    public void RemoveWrapperExceptions()
    {
      _client.RemoveWrapperExceptions(typeof(TargetInvocationException));

      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(1, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(wrapper));
    }

    [Test]
    public void StripAggregateException()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      AggregateException wrapper = new AggregateException(_exception, exception2);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(2, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
      Assert.That(exceptions, Contains.Item(exception2));
    }

    [Test]
    public void StripAggregateExceptionAndTargetInvocationException()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      TargetInvocationException innerWrapper = new TargetInvocationException(exception2);
      AggregateException wrapper = new AggregateException(_exception, innerWrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(2, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
      Assert.That(exceptions, Contains.Item(exception2));
    }

    [Test]
    public void StripTargetInvocationExceptionAndAggregateException()
    {
      _client.AddWrapperExceptions(typeof(AggregateException));

      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      AggregateException innerWrapper = new AggregateException(_exception, exception2);
      TargetInvocationException wrapper = new TargetInvocationException(innerWrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(2, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
      Assert.That(exceptions, Contains.Item(exception2));
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
      Assert.That(3, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(_exception));
      Assert.That(exceptions, Contains.Item(exception2));
      Assert.That(exceptions, Contains.Item(exception3));
    }

    [Test]
    public void StripReflectionTypeLoadException()
    {
      _client.AddWrapperExceptions(typeof(ReflectionTypeLoadException));

      FileNotFoundException ex1 = new FileNotFoundException();
      FileNotFoundException ex2 = new FileNotFoundException();
      ReflectionTypeLoadException wrapper = new ReflectionTypeLoadException(new Type[] { typeof(FakeRaygunWebApiClient), typeof(WrapperException) }, new Exception[] { ex1, ex2 });

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(2, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(ex1));
      Assert.That(exceptions, Contains.Item(ex2));

      Assert.That(ex1.Data["Type"].ToString().Contains("FakeRaygunWebApiClient"), Is.True);
      Assert.That(ex2.Data["Type"].ToString().Contains("WrapperException"), Is.True);
    }
  }
}
