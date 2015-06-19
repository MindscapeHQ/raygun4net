using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Tests;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Mvc.Tests
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
      Assert.AreEqual(1, exceptions.Count);
      Assert.Contains(_exception, exceptions);
    }

    [Test]
    public void StripHttpUnhandledExceptionByDefault()
    {
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);

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
      HttpUnhandledException wrapper = new HttpUnhandledException();

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
      HttpUnhandledException wrapper = new HttpUnhandledException("Something went wrong", _exception);
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
      ReflectionTypeLoadException wrapper = new ReflectionTypeLoadException(new Type[] { typeof(FakeRaygunClient), typeof(WrapperException) }, new Exception[] { ex1, ex2 });

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.Contains(ex1, exceptions);
      Assert.Contains(ex2, exceptions);

      Assert.IsTrue(ex1.Data["Type"].ToString().Contains("FakeRaygunClient"));
      Assert.IsTrue(ex2.Data["Type"].ToString().Contains("WrapperException"));
    }
  }
}
