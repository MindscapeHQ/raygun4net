using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using NUnit.Framework;

namespace Mindscape.Raygun4Net4.Tests
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
    public void StripHttpUnhandledExceptionByDefault()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);

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
      HttpUnhandledException wrapper = new HttpUnhandledException();

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
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);
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
      ReflectionTypeLoadException wrapper = new ReflectionTypeLoadException(new Type[] { typeof(FakeRaygunClient), typeof(WrapperException) }, new Exception[] { ex1, ex2 });

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.That(2, Is.EqualTo(exceptions.Count));
      Assert.That(exceptions, Contains.Item(ex1));
      Assert.That(exceptions, Contains.Item(ex2));

      Assert.That(ex1.Data["Type"].ToString().Contains("FakeRaygunClient"), Is.True);
      Assert.That(ex2.Data["Type"].ToString().Contains("WrapperException"), Is.True);
    }
  }
}
