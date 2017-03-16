using System;
using NUnit.Framework;
using Mindscape.Raygun4Net.Messages;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Xamarin.iOS.Tests
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

    // Exception stripping tests

    [Test]
    public void StripTargetInvocationExceptionByDefault()
    {
      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
    }

    [Test]
    public void StripAggregateExceptionByDefault()
    {
      AggregateException wrapper = new AggregateException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
    }

    [Test]
    public void StripSpecifiedWrapperException()
    {
      _client.AddWrapperExceptions(typeof(WrapperException));

      WrapperException wrapper = new WrapperException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
    }

    [Test]
    public void DontStripIfNoInnerException()
    {
      TargetInvocationException wrapper = new TargetInvocationException(null);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(wrapper));
    }

    [Test]
    public void DontStripNull()
    {
      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(null).ToList();
      Assert.AreEqual(1, exceptions.Count); // The current expected behaviour is that you can pass null to the Send methods and cause Raygun to send a report.
      Assert.IsTrue(exceptions.Contains(null));
    }

    [Test]
    public void StripMultipleWrapperExceptions()
    {
      _client.AddWrapperExceptions(typeof(WrapperException));

      WrapperException wrapper = new WrapperException(_exception);
      TargetInvocationException wrapper2 = new TargetInvocationException(wrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper2).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
    }

    [Test]
    public void RemoveWrapperExceptions()
    {
      _client.RemoveWrapperExceptions(typeof(TargetInvocationException));

      TargetInvocationException wrapper = new TargetInvocationException(_exception);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(1, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(wrapper));
    }

    [Test]
    public void StripAggregateException()
    {
      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      AggregateException wrapper = new AggregateException(_exception, exception2);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
      Assert.IsTrue(exceptions.Contains(exception2));
    }

    [Test]
    public void StripAggregateExceptionAndTargetInvocationException()
    {
      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      TargetInvocationException innerWrapper = new TargetInvocationException(exception2);
      AggregateException wrapper = new AggregateException(_exception, innerWrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
      Assert.IsTrue(exceptions.Contains(exception2));
    }

    [Test]
    public void StripTargetInvocationExceptionAndAggregateException()
    {
      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      AggregateException innerWrapper = new AggregateException(_exception, exception2);
      TargetInvocationException wrapper = new TargetInvocationException(innerWrapper);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(2, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
      Assert.IsTrue(exceptions.Contains(exception2));
    }

    [Test]
    public void StripNestedAggregateExceptions()
    {
      OutOfMemoryException exception2 = new OutOfMemoryException("Ran out of Int64s");
      NotSupportedException exception3 = new NotSupportedException("Forgot to implement this method");
      AggregateException innerWrapper = new AggregateException(_exception, exception2);
      AggregateException wrapper = new AggregateException(innerWrapper, exception3);

      List<Exception> exceptions = _client.ExposeStripWrapperExceptions(wrapper).ToList();
      Assert.AreEqual(3, exceptions.Count);
      Assert.IsTrue(exceptions.Contains(_exception));
      Assert.IsTrue(exceptions.Contains(exception2));
      Assert.IsTrue(exceptions.Contains(exception3));
    }

    private class MyTestException : Exception
    {
      private string myStackTrace;

      public MyTestException(string message, string stackTrace) : base(message)
      {
        myStackTrace = stackTrace;
      }

      public override string StackTrace { get { return myStackTrace; } }
    }

    [Test]
    public void FormatStacktraceWithDebugSymbolsTest()
    {
      string stackTraceStr = @" at XamarinIOSSingleViewTest.AppDelegate+<Bar>d__8.MoveNext () [0x000ad] in C:\Dev\Tests\XamarinIOSTestApplication\XamarinIOSSingleViewTest\AppDelegate.cs:68 
--- End of stack trace from previous location where exception was thrown ---
  at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw () [0x0000c] in /Library/Frameworks/Xamarin.iOS.framework/Versions/10.4.0.123/src/mono/mcs/class/referencesource/mscorlib/system/runtime/exceptionservices/exceptionservicescommon.cs:143 
  at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (System.Threading.Tasks.Task task) [0x00047] in /Library/Frameworks/Xamarin.iOS.framework/Versions/10.4.0.123/src/mono/mcs/class/referencesource/mscorlib/system/runtime/compilerservices/TaskAwaiter.cs:187 
  at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (System.Threading.Tasks.Task task) [0x0002e] in /Library/Frameworks/Xamarin.iOS.framework/Versions/10.4.0.123/src/mono/mcs/class/referencesource/mscorlib/system/runtime/compilerservices/TaskAwaiter.cs:156 
  at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd (System.Threading.Tasks.Task task) [0x0000b] in /Library/Frameworks/Xamarin.iOS.framework/Versions/10.4.0.123/src/mono/mcs/class/referencesource/mscorlib/system/runtime/compilerservices/TaskAwaiter.cs:128 
  at System.Runtime.CompilerServices.TaskAwaiter.GetResult () [0x00000] in /Library/Frameworks/Xamarin.iOS.framework/Versions/10.4.0.123/src/mono/mcs/class/referencesource/mscorlib/system/runtime/compilerservices/TaskAwaiter.cs:113 
  at XamarinIOSSingleViewTest.AppDelegate+<Foo>d__7.MoveNext () [0x000cf] in C:\Dev\Tests\XamarinIOSTestApplication\XamarinIOSSingleViewTest\AppDelegate.cs:58";
      
      MyTestException exception = new MyTestException("Exception test message", stackTraceStr);
      var message = RaygunErrorMessageBuilder.Build(exception);

      foreach (var line in message.StackTrace)
      {
        Console.WriteLine(line.ToString() + "\n");
      }

      Assert.IsNotEmpty(message.ClassName);
      Assert.IsNotNull(message.StackTrace);
      Assert.IsNotEmpty(message.StackTrace);

      var lineOne = message.StackTrace.FirstOrDefault();
      Assert.IsNotNullOrEmpty(lineOne.ClassName);
      Assert.IsNotNullOrEmpty(lineOne.MethodName);
    }

    [Test]
    public void FormatStacktraceWithoutDebugSymbolsTest()
    {
      string stackTraceStr = @"  at MyStudyLife.Net.HttpApiClient+<EnsureSuccessAsync>c__async7.MoveNext () <0xc2b59c + 0x00670> in <06b44c3216694065a06dc13af99a121f#a73e7e6904809cacfd9634d844fe30fc>
--- End of stack trace from previous location where exception was thrown ---
  at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (System.Threading.Tasks.Task task) <0x47ec74 + 0x00118> in <15e850188d9f425bbeae90f0bbc51e17#a73e7e6904809cacfd9634d844fe30fc>
  at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (System.Threading.Tasks.Task task) <0x47ebac + 0x000bf> in <15e850188d9f425bbeae90f0bbc51e17#a73e7e6904809cacfd9634d844fe30fc>
  at System.Runtime.CompilerServices.TaskAwaiter.GetResult () <0x47eb68 + 0x0001f> in <15e850188d9f425bbeae90f0bbc51e17#a73e7e6904809cacfd9634d844fe30fc>
  at MyStudyLife.Net.HttpApiClient+<GetClientResponse>c__async6.MoveNext () <0xb89040 + 0x00407> in <06b44c3216694065a06dc13af99a121f#a73e7e6904809cacfd9634d844fe30fc>
--- End of stack trace from previous location where exception was thrown ---
  at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (System.Threading.Tasks.Task task) <0x47ec74 + 0x00118> in <15e850188d9f425bbeae90f0bbc51e17#a73e7e6904809cacfd9634d844fe30fc>
  at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (System.Threading.Tasks.Task task) <0x47ebac + 0x000bf> in <15e850188d9f425bbeae90f0bbc51e17#a73e7e6904809cacfd9634d844fe30fc>
  at System.Runtime.CompilerServices.TaskAwaiter`1[TResult].GetResult () <0x47ee98 + 0x0002f> in <15e850188d9f425bbeae90f0bbc51e17#a73e7e6904809cacfd9634d844fe30fc>
  at MyStudyLife.Net.HttpApiClient+<PostJsonAsync>c__async2.MoveNext () <0xb88598 + 0x0019b> in <06b44c3216694065a06dc13af99a121f#a73e7e6904809cacfd9634d844fe30fc>";

      MyTestException exception = new MyTestException("Exception test message", stackTraceStr);
      var message = RaygunErrorMessageBuilder.Build(exception);

      foreach (var line in message.StackTrace)
      {
        Console.WriteLine(line.ToString() + "\n");
      }

      Assert.IsNotEmpty(message.ClassName);
      Assert.IsNotNull(message.StackTrace);
      Assert.IsNotEmpty(message.StackTrace);

      var lineOne = message.StackTrace.FirstOrDefault();
      Assert.IsNotNullOrEmpty(lineOne.ClassName);
      Assert.IsNotNullOrEmpty(lineOne.MethodName);
    }
  }
}
