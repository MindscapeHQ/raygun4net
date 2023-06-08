using System;
using System.Diagnostics;
using System.Linq;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net4.Nuget.Tests.Setup;

namespace Mindscape.Raygun4Net4.Nuget.Tests
{
  public class TestIfWeCanSendToRaygun
  {
    private const string ApiKey = "5lsjIVQDYTSdWppeJzlJw";//"PUT_YOUR_KEY_HERE"
    private const string InvalidApiKey = "BADKEY";
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

    /// <summary>
    /// This checks that a local user secret has been set for this project
    ///
    /// Make your you set the api key to a real key, but don't commit it!
    /// </summary>
    [Test]
    public void CheckApiKeyHasBeenSet()
    {
      var sut = new RaygunClient(ApiKey);
      sut.Send(new NotImplementedException(nameof(CanSendWithValidKey)));

      Assert.IsNotEmpty(ApiKey);
      Assert.IsNull(TraceChecker.Traces.FirstOrDefault(f=> f.Equals("Raygun: Failed to send offline reports due to invalid API key.")));
    }


    [Test]
    public void CanSendWithValidKey()
    {
      var sut = new RaygunClient(ApiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithValidKey)));

      Assert.IsNull(TraceChecker.Traces.FirstOrDefault());
    }

    [Test]
    public void CanSendWithInvalidKeyButGetsA403()
    {
      var sut = new RaygunClient(InvalidApiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithInvalidKeyButGetsA403)));

      Assert.Contains("Raygun: Failed to send report to Raygun due to: The remote server returned an error: (403) Forbidden.", TraceChecker.Traces);
    }

    [TestCase(ApiKey)]
    [TestCase(InvalidApiKey)]
    public void CanTrySendAndHasNoSecureChannelErrors(string apiKey)
    {
      var sut = new RaygunClient(apiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithInvalidKeyButGetsA403)));

      Assert.IsFalse(TraceChecker.Traces.Contains("Raygun: Failed to send report to Raygun due to: The request was aborted: Could not create SSL/TLS secure channel."));
    }
  }
}