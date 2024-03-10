using System;
using System.Collections.Generic;
using System.Threading;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class RaygunClientBaseTests
  {
    private FakeRaygunClient _client = new();

    [Test]
    public void FlagAsSentDoesNotCrash_DataDoesNotSupportStringKeys()
    {
      Assert.That(() => _client.ExposeFlagAsSent(new FakeException(new Dictionary<int, object>())), Throws.Nothing);
    }

    [Test]
    public void FlagAsSentDoesNotCrash_NullData()
    {
      Assert.That(() => _client.ExposeFlagAsSent(new FakeException(null)), Throws.Nothing);
    }

    [Test]
    public void CanSendIfDataIsNull()
    {
      Assert.That(_client.ExposeCanSend(new FakeException(null)), Is.True);
    }

    [Test]
    public void CannotSendSentException_StringDictionary()
    {
      Exception exception = new FakeException(new Dictionary<string, object>());
      _client.ExposeFlagAsSent(exception);
      Assert.That(_client.ExposeCanSend(exception), Is.False);
    }

    [Test]
    public void CannotSendSentException_ObjectDictionary()
    {
      Exception exception = new FakeException(new Dictionary<object, object>());
      _client.ExposeFlagAsSent(exception);
      Assert.That(_client.ExposeCanSend(exception), Is.False);
    }

    [Test]
    public void ExceptionInsideSendingMessageHAndlerDoesNotCrash()
    {
      FakeRaygunClient client = new FakeRaygunClient();
      client.SendingMessage += (sender, args) => { throw new Exception("Oops..."); };

      Assert.That(() => client.ExposeOnSendingMessage(new RaygunMessage()), Throws.Nothing);
      Assert.That(client.ExposeOnSendingMessage(new RaygunMessage()), Is.True);
    }

    [Test]
    public void RaygunClient_DoesNotCauseMemoryLeak_WhenUnhandledExceptionsAreSubscribed()
    {
      var weakRef = null as WeakReference;

      new Action(() =>
      {
        // Run this in a delegate to that the local variable gets garbage collected
        var client = new FakeRaygunClient();
        weakRef = new WeakReference(client);
      })();

      UnhandledExceptionBridge.RaiseUnhandledException(new Exception("Something bad"), false);
      Assert.That(weakRef.IsAlive, Is.True);

      // Force a GC
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      Assert.That(weakRef.IsAlive, Is.False);
    }

    [Test]
    public void RaygunClient_Works_WhenUnhandledExceptionsAreSubscribed()
    {
      RaygunMessage message = null;
      var manualResetEvent = new ManualResetEvent(false);
      var client = new FakeRaygunClient(new RaygunSettings
      {
        ApiKey = "test",
        CatchUnhandledExceptions = true
      });

      client.SendingMessage += (sender, args) =>
      {
        message = args.Message;
        manualResetEvent.Set();
      };

      UnhandledExceptionBridge.RaiseUnhandledException(new Exception("Something bad"), false);

      manualResetEvent.WaitOne(5000);

      Assert.That(message, Is.Not.Null);
    }
  }
}