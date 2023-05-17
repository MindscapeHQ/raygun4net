using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net4.Integration.Tests.Setup;
using NUnit.Framework;



namespace Mindscape.Raygun4Net4.Integration.Tests
{
  public class TestCurrentTls
  {
    private TraceChecker TraceChecker { get; } = new();

    [SetUp]
    public void Setup()
    {
      Trace.Listeners.Clear();
      TraceChecker.Clear();
      Trace.Listeners.Add(new ConsoleTraceListener());
      Trace.Listeners.Add(TraceChecker);
    }

    [TearDown]
    public void EndTest()
    {
      Trace.Flush();
    }


#if NET46_OR_GREATER

    [Test]
    public void CanSendIfNet46AndIfTLSSet()
    {
      var sut = new RaygunClient("BADKEY");

      sut.Send(new NotImplementedException(nameof(CanSendIfNet46AndIfTLSSet)));
      Assert.Contains("Raygun: Failed to send report to Raygun due to: The remote server returned an error: (403) Forbidden.", TraceChecker.Traces);
    }

#endif




#if NET46_OR_GREATER
    [Test]
    public void CanSendIfNet46PlusAndIfTLSNOTManuallySet()
    {
      var sut = new RaygunClient("BADKEY");

      sut.Send(new NotImplementedException(nameof(CanSendIfNet46PlusAndIfTLSNOTManuallySet)));
      Assert.Contains("Raygun: Failed to send report to Raygun due to: The remote server returned an error: (403) Forbidden.", TraceChecker.Traces);
    }

#elif NET45
    [Test]
    public void CanSendIfNet45AndIfTLSNOTManuallySet()
    {
      var sut = new RaygunClient("BADKEY");

      sut.Send(new NotImplementedException(nameof(CanSendIfNet45AndIfTLSNOTManuallySet)));
      Assert.Contains("Raygun: Failed to send report to Raygun due to: The request was aborted: Could not create SSL/TLS secure channel.", TraceChecker.Traces);
    }
#else
    [Test]
    public void CanNOTSendIfNet4AndTLSNOTManuallySet()
    {
      var sut = new RaygunClient("BADKEY");

      sut.Send(new NotImplementedException(nameof(CanNOTSendIfNet4AndTLSNOTManuallySet)));
      Assert.Contains("Raygun: Failed to send report to Raygun due to: The request was aborted: Could not create SSL/TLS secure channel.", TraceChecker.Traces);
    }
#endif



#if NET45_OR_GREATER


   //noop

#elif NET45

    [Test]
    public void CanSendIfNet45AndIfTLSManuallySet()
    {
      var holdingpen = ServicePointManager.SecurityProtocol;
      ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11;

      var sut = new RaygunClient("BADKEY");

      sut.Send(new NotImplementedException(nameof(CanSendIfNet45AndIfTLSManuallySet)));
      Assert.Contains("Raygun: Failed to send report to Raygun due to: The remote server returned an error: (403) Forbidden.", TraceChecker.Traces);

      ServicePointManager.SecurityProtocol = holdingpen;
    }

#else

    [Test]
    public void CanSendIfNet4AndIfTLSManuallySetToInt()
    {
      var holdingpen = ServicePointManager.SecurityProtocol;
      ServicePointManager.SecurityProtocol |= (SecurityProtocolType)768;

      var sut = new RaygunClient("BADKEY");

      sut.Send(new NotImplementedException(nameof(CanSendIfNet4AndIfTLSManuallySetToInt)));
      Assert.Contains("Raygun: Failed to send report to Raygun due to: The remote server returned an error: (403) Forbidden.", TraceChecker.Traces);

      ServicePointManager.SecurityProtocol = holdingpen;
    }

#endif
  }

}