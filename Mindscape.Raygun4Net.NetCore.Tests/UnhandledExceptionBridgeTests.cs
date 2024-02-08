using System;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class UnhandledExceptionBridgeTests
  {
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
        // Manually raise an unhandled exception, to handlers that are not alive
        // Which should cause the lock upgrade exception
        UnhandledExceptionBridge.RaiseUnhandledException(new Exception("Dead"), false);
      }
      catch (Exception ex)
      {
        observedException = ex;
      }
      
      Assert.That(observedException, Is.Null);
    }
  }
}