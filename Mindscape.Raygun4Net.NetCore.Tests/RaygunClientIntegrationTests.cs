using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Offline;

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
    [NonParallelizable]
    public async Task SendInBackground_WhenDiskCheckHangs_StillSendsReportMarkedTimedOut()
    {
      // Match the body when the request is sent: the client disposes the request content afterwards
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries")
        .PartialBody("\"DiskSpaceFreeStatus\":\"TimedOut\""))
        .Respond(x =>
        {
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana"
      }, _httpClient);

      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      try
      {
        var send = Task.Run(() => client.SendInBackground(new Exception("Test Exception while the disk check hangs")));

        (await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(send, "SendInBackground should only wait for the disk check up to the time limit");
        await send;

        // Delay 1 second to give it time to send the message
        await Task.Delay(1000);
      }
      finally
      {
        gate.Set();
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      // Fails if the only request sent did not contain the expected body
      _mockHttp.Verify();
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(1));
    }

    [Test]
    [NonParallelizable]
    public async Task SendAsync_WhenDiskCheckHangs_StillSendsReportMarkedTimedOut()
    {
      // Match the body when the request is sent: the client disposes the request content afterwards
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries")
        .PartialBody("\"DiskSpaceFreeStatus\":\"TimedOut\""))
        .Respond(x =>
        {
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana"
      }, _httpClient);

      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      try
      {
        var send = Task.Run(() => client.SendAsync(new Exception("Test Exception while the disk check hangs")));

        (await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(send, "SendAsync should only wait for the disk check up to the time limit");
        await send;
      }
      finally
      {
        gate.Set();
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      // SendAsync swallows exceptions by default, so a failure to build the message would only show as nothing being sent
      // Fails if the only request sent did not contain the expected body
      _mockHttp.Verify();
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(1));
    }

    [Test]
    [NonParallelizable]
    public async Task SendAsync_WhenOfflineAndDiskCheckHangs_SavesReportToOfflineStore()
    {
      // The #584 scenario: the machine is offline, so the report should land in the offline store rather than be lost
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"))
        .Throws<HttpRequestException>();

      _httpClient = new HttpClient(_mockHttp);

      var store = new RecordingOfflineStore();
      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana",
        OfflineStore = store
      }, _httpClient);

      using var gate = new ManualResetEventSlim(false);
      UseHangingDiskProvider(gate);

      try
      {
        var send = Task.Run(() => client.SendAsync(new Exception("Test Exception while offline and the disk check hangs")));

        (await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(send, "SendAsync should only wait for the disk check up to the time limit");
        await send;
      }
      finally
      {
        gate.Set();
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      store.SavedPayloads.Should().ContainSingle()
           .Which.Should().Contain("\"DiskSpaceFreeStatus\":\"TimedOut\"");
    }

    [Test]
    [NonParallelizable]
    public async Task SendAsync_WhenDiskSpaceIgnored_SendsReportMarkedIgnored()
    {
      // Match the body when the request is sent: the client disposes the request content afterwards
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries")
        .PartialBody("\"DiskSpaceFree\":[]")
        .PartialBody("\"DiskSpaceFreeStatus\":\"Ignored\""))
        .Respond(x =>
        {
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana",
        IsDiskSpaceFreeIgnored = true
      }, _httpClient);

      RaygunEnvironmentMessageBuilder.ResetForTests();
      try
      {
        await client.SendAsync(new Exception("Test Exception with disk space ignored"));
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      // Fails if the only request sent did not contain the expected body
      _mockHttp.Verify();
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(1));
    }

    [Test]
    [NonParallelizable]
    public async Task SendInBackground_WhenDiskCheckFails_SendsReportMarkedError()
    {
      // Match the body when the request is sent: the client disposes the request content afterwards
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries")
        .PartialBody("\"DiskSpaceFree\":[]")
        .PartialBody("\"DiskSpaceFreeStatus\":\"Error\""))
        .Respond(x =>
        {
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana"
      }, _httpClient);

      RaygunEnvironmentMessageBuilder.ResetForTests();
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");
      try
      {
        await client.SendInBackground(new Exception("Test Exception when the disk check fails"));

        // Delay 1 second to give it time to send the message
        await Task.Delay(1000);
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      // Fails if the only request sent did not contain the expected body
      _mockHttp.Verify();
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(1));
    }

    [Test]
    [NonParallelizable]
    public async Task SendAsync_WhenOfflineAndDiskCheckFails_SavesReportMarkedError()
    {
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"))
        .Throws<HttpRequestException>();

      _httpClient = new HttpClient(_mockHttp);

      var store = new RecordingOfflineStore();
      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana",
        OfflineStore = store
      }, _httpClient);

      RaygunEnvironmentMessageBuilder.ResetForTests();
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");
      try
      {
        await client.SendAsync(new Exception("Test Exception while offline and the disk check fails"));
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      store.SavedPayloads.Should().ContainSingle()
           .Which.Should().Contain("\"DiskSpaceFreeStatus\":\"Error\"");
    }

    [Test]
    [NonParallelizable]
    public async Task SendAsync_WhenDiskCheckFails_SendsReportMarkedError()
    {
      // Match the body when the request is sent: the client disposes the request content afterwards
      _mockHttp.When(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries")
        .PartialBody("\"DiskSpaceFree\":[]")
        .PartialBody("\"DiskSpaceFreeStatus\":\"Error\""))
        .Respond(x =>
        {
          x.Body("OK");
          x.StatusCode(HttpStatusCode.Accepted);
        }).Verifiable();

      _httpClient = new HttpClient(_mockHttp);

      var client = new BananaClient(new RaygunSettings
      {
        ApiKey = "banana"
      }, _httpClient);

      RaygunEnvironmentMessageBuilder.ResetForTests();
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () => throw new IOException("Disk error");
      try
      {
        await client.SendAsync(new Exception("Test Exception when the disk check fails"));
      }
      finally
      {
        RaygunEnvironmentMessageBuilder.ResetForTests();
      }

      // Fails if the only request sent did not contain the expected body
      _mockHttp.Verify();
      await _mockHttp.VerifyAsync(match => match.Method(HttpMethod.Post)
        .RequestUri("https://api.raygun.com/entries"), IsSent.Exactly(1));
    }

    private static void UseHangingDiskProvider(ManualResetEventSlim gate)
    {
      RaygunEnvironmentMessageBuilder.ResetForTests();
      RaygunEnvironmentMessageBuilder.DiskSpaceTimeout = TimeSpan.FromMilliseconds(300);
      RaygunEnvironmentMessageBuilder.DiskSpaceProvider = () =>
      {
        gate.Wait();
        return new List<double> { 42 };
      };
    }

    private class RecordingOfflineStore : OfflineStoreBase
    {
      public ConcurrentBag<string> SavedPayloads { get; } = new();

      public RecordingOfflineStore() : base(new NoOpSendStrategy())
      {
      }

      public override Task<List<CrashReportStoreEntry>> GetAll(CancellationToken cancellationToken)
      {
        return Task.FromResult(new List<CrashReportStoreEntry>());
      }

      public override Task<bool> Save(string crashPayload, string apiKey, CancellationToken cancellationToken)
      {
        SavedPayloads.Add(crashPayload);
        return Task.FromResult(true);
      }

      public override Task<bool> Remove(Guid cacheEntryId, CancellationToken cancellationToken)
      {
        return Task.FromResult(true);
      }
    }

    private class NoOpSendStrategy : IBackgroundSendStrategy
    {
      public event Func<Task> OnSendAsync;

      public void Start()
      {
      }

      public void Stop()
      {
      }

      public void Dispose()
      {
      }
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