using System;
using System.Diagnostics;
using System.Linq;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net4.Integration.Tests.Setup;


namespace Mindscape.Raygun4Net4.Integration.Tests
{
  public class TestIfWeCanSendToRaygun
  {
    private string ApiKey = "";//"PUT_YOUR_KEY_HERE"
    private const string InvalidApiKey = "BADKEY";
    private TraceChecker TraceChecker { get; } = new();


    [SetUp]
    public void Setup()
    {
      if (ApiKey == "")
      {
        if (Environment.GetEnvironmentVariables().Contains("RaygunApiKey"))
        {
          ApiKey = Environment.GetEnvironmentVariable("RaygunApiKey")!;
        }
        else
        {
          Assert.Fail("You need to set the ApiKey to a valid key in order to run these tests");
        }
      }

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

      Assert.That(ApiKey, Is.Not.Empty);
      Assert.That(TraceChecker.Traces.FirstOrDefault(f=> f.Equals("Raygun: Failed to send offline reports due to invalid API key.")), Is.Null);
    }


    [Test]
    public void CanSendWithValidKey()
    {
      var sut = new RaygunClient(ApiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithValidKey)));

      Assert.That(TraceChecker.Traces.FirstOrDefault(), Is.Null);
    }

    [Test]
    public void CanSendWithInvalidKeyButGetsA403()
    {
      var sut = new RaygunClient(InvalidApiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithInvalidKeyButGetsA403)));

      Assert.That(TraceChecker.Traces, Contains.Item("Raygun: Failed to send report to Raygun due to: The remote server returned an error: (403) Forbidden."));
    }

    [Test]
    public void CanTrySendAndHasNoSecureChannelErrors_ValidKey()
    {
      var sut = new RaygunClient(ApiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithInvalidKeyButGetsA403)));

      Assert.That(TraceChecker.Traces.Contains("Raygun: Failed to send report to Raygun due to: The request was aborted: Could not create SSL/TLS secure channel."), Is.False);
    }

    [Test]
    public void CanTrySendAndHasNoSecureChannelErrors_InvalidKey()
    {
      var sut = new RaygunClient(InvalidApiKey);

      sut.Send(new NotImplementedException(nameof(CanSendWithInvalidKeyButGetsA403)));

      Assert.That(TraceChecker.Traces.Contains("Raygun: Failed to send report to Raygun due to: The request was aborted: Could not create SSL/TLS secure channel."), Is.False);
    }
  }
}