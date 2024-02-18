using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MockHttp;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class RaygunClientIntegrationTests
  {
    private HttpClient _httpClient = null!;
    private MockHttpHandler _mockHttp = null!;

    public class BananaClient : RaygunClient
    {
      public BananaClient(RaygunSettings settings, HttpClient httpClient) : base(settings, httpClient)
      {
      }
    }

    [SetUp]
    public void Init()
    {
      _mockHttp = new MockHttpHandler();
    }

    [Test]
    public async Task SendInBackground_ShouldNotBlock()
    {
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"))
        .Respond(x => 
        {
          x.Latency(NetworkLatency.Between(5, 15));
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      // This test runs using a mocked http client so we don't need a real API key, if you want to run this
      // and have the data sent to Raygun, remove the `httpClient` parameter from the RaygunClient constructor
      // and set a real API key. This will then send the data to Raygun and you can verify that it is being sent.
      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana"
      }, _httpClient);

      var stopwatch = new Stopwatch();
      
      for (int i = 0; i < 50; i++)
      {
        try
        {
          throw new Exception("Test Exception to ensure SendInBackground is not blocking");
        }
        catch (Exception e)
        {
          await client.SendInBackground(e);
        }
      }
      
      var elapsed = stopwatch.ElapsedMilliseconds;
      
      // 50 * 300 = 15000ms
      // If the requests were blocking it would take minimum 15 seconds at minimum 300ms per request to send each one
      // So we can assume that the requests are being sent in parallel, this should be a lot less than 15 seconds
      Assert.That(elapsed, Is.LessThan(100));
      
      Console.WriteLine("Elapsed: " + elapsed + "ms");

      // Delay 1 second to give it time to send all the messages
      await Task.Delay(1000);

      // Verify that the request was sent 50 times
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(50));
    }
    
    [Test]
    public async Task SendInBackground_ShouldFail_WhenMaxTasksIsZero()
    {
      _mockHttp.When(match => match.Method(HttpMethod.Post)
          .RequestUri("https://api.raygun.com/entries"))
        .Respond(x => 
        {
          x.Latency(NetworkLatency.Between(5, 15));
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      // This test runs using a mocked http client so we don't need a real API key, if you want to run this
      // and have the data sent to Raygun, remove the `httpClient` parameter from the RaygunClient constructor
      // and set a real API key. This will then send the data to Raygun and you can verify that it is being sent.
      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana",
        BackgroundMessageWorkerCount = 0
      }, _httpClient);

      var stopwatch = new Stopwatch();
      
      for (int i = 0; i < 50; i++)
      {
        try
        {
          throw new Exception("Test Exception to ensure SendInBackground is not blocking");
        }
        catch (Exception e)
        {
          await client.SendInBackground(e);
        }
      }
      
      var elapsed = stopwatch.ElapsedMilliseconds;
      
      // 50 * 300 = 15000ms
      // If the requests were blocking it would take minimum 15 seconds at minimum 300ms per request to send each one
      // So we can assume that the requests are being sent in parallel, this should be a lot less than 15 seconds
      Assert.That(elapsed, Is.LessThan(100));
      
      Console.WriteLine("Elapsed: " + elapsed + "ms");

      // Delay 1 second to give it time to send all the messages
      await Task.Delay(1000);

      // Verify that the request wasn't sent because there was no worker
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(0));
    }
    
    [Test]
    [NonParallelizable]
    public async Task SendInBackground_WithLowQueueMax_DoesNotSendAllRequests()
    {
      Environment.SetEnvironmentVariable("RAYGUN_MESSAGE_QUEUE_MAX", "10");
      
      _mockHttp.When(match => match.Method(HttpMethod.Post)
          .RequestUri("https://api.raygun.com/entries"))
        .Respond(x => 
        {
          x.Latency(NetworkLatency.Between(5, 15));
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      // This test runs using a mocked http client so we don't need a real API key, if you want to run this
      // and have the data sent to Raygun, remove the `httpClient` parameter from the RaygunClient constructor
      // and set a real API key. This will then send the data to Raygun and you can verify that it is being sent.
      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana"
      }, _httpClient);

      var stopwatch = new Stopwatch();
      
      for (int i = 0; i < 50; i++)
      {
        try
        {
          throw new Exception("Test Exception to ensure SendInBackground is not blocking");
        }
        catch (Exception e)
        {
          await client.SendInBackground(e);
        }
      }
      
      var elapsed = stopwatch.ElapsedMilliseconds;
      
      // 50 * 300 = 15000ms
      // If the requests were blocking it would take minimum 15 seconds at minimum 300ms per request to send each one
      // So we can assume that the requests are being sent in parallel, this should be a lot less than 15 seconds
      Assert.That(elapsed, Is.LessThan(100));
      
      Console.WriteLine("Elapsed: " + elapsed + "ms");

      // Delay 1 second to give it time to send all the messages
      await Task.Delay(1000);

      // Verify that the request was sent 50 times
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.AtMost(49));
      
      Environment.SetEnvironmentVariable("RAYGUN_MESSAGE_QUEUE_MAX", null);
    }
  }
}