using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MockHttp;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

[TestFixture]
public class RaygunMiddlewareTests
{
  private HttpClient _httpClient = null!;
  private MockHttpHandler _mockHttp = null!;
  private IHost _host = null!;
  private HttpClient _client = null!;

  [SetUp]
  public async Task Init()
  {
    _mockHttp = new MockHttpHandler();
    _httpClient = new HttpClient(_mockHttp);

    var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
    {
      webBuilder.UseTestServer()
                .ConfigureServices((_, services) =>
                {
                  services.AddRouting();
                  services.AddSingleton<RaygunClient>(s => new RaygunClient(s.GetService<RaygunSettings>(), _httpClient));
                  services.AddRaygun(configure: settings =>
                  {
                    settings.ApiKey = "banana";
                    settings.ExcludedStatusCodes = new[] { (int)HttpStatusCode.NotFound, (int)HttpStatusCode.BadRequest };
                    settings.IgnoreHeaderNames.Add("Banana");
                  });
                })
                .Configure(app =>
                {
                  app.UseRouting();
                  app.UseRaygun();
                  app.UseEndpoints(endpoints =>
                  {
                    endpoints.MapGet("/", async context => await context.Response.WriteAsync("Hello World!"));
                    endpoints.MapGet("/test-exception", new Func<object>(() => throw new Exception("Banana's are indeed yellow")));
                    endpoints.MapGet("/404", context =>
                    {
                      context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                      throw new Exception("Not Found");
                    });
                    endpoints.MapGet("/400", context =>
                    {
                      context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                      throw new Exception("Bad Request");
                    });
                    endpoints.MapGet("handled", ([FromServices] RaygunClient client) =>
                    {
                      try
                      {
                        throw new Exception("I should be handled only once...");
                      }
                      catch (Exception ex)
                      {
                        client.SendInBackground(ex);
                        throw;
                      }
                    });
                  });
                });
    });
    
    _host = await builder.StartAsync();

    _client = _host.GetTestClient();
  }

  [TearDown]
  public void UnInit()
  {
    _httpClient.Dispose();
    _mockHttp.Dispose();
    _host.Dispose();
    _client.Dispose();
  }
  
  [OneTimeSetUp]
  public void StartTest()
  {
    Trace.Listeners.Add(new ConsoleTraceListener());
  }

  [OneTimeTearDown]
  public void EndTest()
  {
    Trace.Flush();
  }

  [Test]
  public async Task WhenExceptionIsThrown_ShouldBeCapturedByMiddleware_EntrySentToRaygun()
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync("/test-exception");

    await act.Should().ThrowAsync<Exception>().WithMessage("Banana's are indeed yellow");

    var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
    
    // Need to give the middleware time to send the message (up to 1 second based on the cancellation token)
    while (!token.IsCancellationRequested && _mockHttp.InvokedRequests.Count == 0)
    {
    }

    _mockHttp.InvokedRequests.Should().HaveCount(1);
  }

  [Test]
  public async Task WhenHeaderContainsIgnoredKey_ShouldStripHeaderFromRequest()
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
             }).Verifiable();


    Func<Task> act = async () => await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "test-exception")
    {
      Headers =
      {
        { "Banana", "yellow" },
        { "Apple", "red" }
      }
    });

    await act.Should().ThrowAsync<Exception>().WithMessage("Banana's are indeed yellow");

    await Task.Delay(TimeSpan.FromSeconds(1));

    _mockHttp.InvokedRequests.Should().HaveCount(1);
    
    var request = _mockHttp.InvokedRequests[0].Request;
    var content = await request.Content?.ReadAsStringAsync()!;

    var raygunMsg = JsonSerializer.Deserialize<RaygunMessage>(content)!;

    raygunMsg.Details.Request.Headers.Keys.Cast<string>().Should().NotContain("Banana");
  }

  [Test]
  public async Task WhenExceptionIsHandled_MiddlewareShouldNotSendItToRaygun()
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync("/handled");

    await act.Should().ThrowAsync<Exception>().WithMessage("I should be handled only once...");

    var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
    
    // Need to give the middleware time to send the message (up to 1 second based on the cancellation token)
    while (!token.IsCancellationRequested && _mockHttp.InvokedRequests.Count == 0)
    {
    }

    // We are verifying the Try/Catch block in the /handled endpoint sends to Raygun
    // and that the middleware did not also send the exception to Raygun. So total sends should be 1
    _mockHttp.InvokedRequests.Should().HaveCount(1);
  }
  
  [Test]
  public async Task WhenNoExceptionThrown_ShouldNotMessWithPayload_AndNotSendToRaygun()
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
             }).Verifiable();

    var response = await _client.GetAsync("/");

    response.IsSuccessStatusCode.Should().BeTrue();
    response.Content.ReadAsStringAsync().Result.Should().Be("Hello World!");

    // Wait 1 second to make sure nothing is sent to Raygun
    await Task.Delay(TimeSpan.FromSeconds(1));

    _mockHttp.InvokedRequests.Should().BeEmpty();
  }
  
  [TestCase(HttpStatusCode.NotFound, "Not Found")]
  [TestCase(HttpStatusCode.BadRequest, "Bad Request")]
  public async Task WhenStatusCodeIsExcluded_ShouldNotSendToRaygun(HttpStatusCode statusCode, string expectedContent)
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync($"/{(int)statusCode}");

    await act.Should().ThrowAsync<Exception>().WithMessage(expectedContent);

    // Wait 1 second to make sure nothing is sent to Raygun
    await Task.Delay(TimeSpan.FromSeconds(1));

    _mockHttp.InvokedRequests.Should().BeEmpty();
  }
  
  [Test]
  public async Task WhenExcludeLocalIsEnabled_ShouldNotSendToRaygun()
  {
    // Test re-init with ExcludeErrorsFromLocal enabled
    var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
    {
      webBuilder.UseTestServer()
                .ConfigureServices((_, services) =>
                {
                  services.AddRouting();
                  services.AddSingleton<RaygunClient>(s => new RaygunClient(s.GetService<RaygunSettings>(), _httpClient));
                  services.AddRaygun(configure: settings =>
                  {
                    settings.ApiKey = "banana";
                    settings.ExcludeErrorsFromLocal = true;
                  });
                })
                .Configure(app =>
                {
                  app.UseRouting();
                  app.UseMiddleware<RaygunMiddleware>();
                  app.UseEndpoints(endpoints =>
                  {
                    endpoints.MapGet("/test-exception", new Func<object>(() => throw new Exception("Banana's are indeed yellow")));
                  });
                });
    });
    
    var host = await builder.StartAsync();
    var client = host.GetTestClient();
    
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
             }).Verifiable();

    Func<Task> act = async () => await client.GetAsync($"/test-exception");

    await act.Should().ThrowAsync<Exception>();

    // Wait 1 second to make sure nothing is sent to Raygun
    await Task.Delay(TimeSpan.FromSeconds(1));

    _mockHttp.InvokedRequests.Should().BeEmpty();
  }
}