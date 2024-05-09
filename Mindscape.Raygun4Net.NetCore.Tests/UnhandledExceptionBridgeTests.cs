using System;
using System.Threading;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class UnhandledExceptionBridgeTests
  {
    private RaygunClientBase _strongRef;

    [SetUp]
    public void Setup()
    {
      _strongRef = new RaygunClient(new RaygunSettings
        {
          CatchUnhandledExceptions = true,
          ApiKey = "dummy"
        }
      );
    }

    /// <summary>
    /// GitHub Issue: 513
    /// See https://github.com/MindscapeHQ/raygun4net/issues/513 for the issue report
    /// </summary>
    [Test]
    public void UnhandledExceptionBridge_WhenHandlersAreNoLongerAlive_LockExceptionsAreNotThrown()
    {
      Exception observedException = null;

      // Need to put this into an action to cause the references to be destroyed on the GC
      new Action(() =>
      {
        UnhandledExceptionBridge.OnUnhandledException(Callback);
        UnhandledExceptionBridge.OnUnhandledException(Callback);
        UnhandledExceptionBridge.OnUnhandledException(Callback);

        return;

        void Callback(Exception e, bool b)
        {
        }
      })();

      // GC the callbacks, causing their handlers to be marked as not alive
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      try
      {
        // Manually raise an exception, to handlers references that are no longer alive.
        UnhandledExceptionBridge.RaiseUnhandledException(new Exception("Dead"), false);
      }
      catch (Exception ex)
      {
        observedException = ex;
      }

      Assert.That(observedException, Is.Null);
    }

    [Test]
    public void UnhandledExceptionBridge_WhenClientStillAlive_DelegateShouldStillBeAlive()
    {
      string errorMessage = null;
      var eventRaised = new ManualResetEvent(false);

      _strongRef.SendingMessage += (_, args) =>
      {
        errorMessage = args.Message.Details.Error.Message;
        eventRaised.Set();
      };

      // Call GC, this should NOT GC the delegate which the UnhandledExceptionBridge holds for the RaygunClient's 
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      UnhandledExceptionBridge.RaiseUnhandledException(new Exception("Alive"), false);

      // Wait until the SendingMessage callback has happened
      const int timeout = 5000; // 5 seconds
      var eventSignaled = eventRaised.WaitOne(timeout);

      Assert.That(eventSignaled, Is.True, "Timeout: Event handler did not complete within the expected time.");
      Assert.That(errorMessage, Is.Not.Null);
    }
  }
}