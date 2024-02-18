using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

  [SetUp]
  public void Init()
  {
    _mockHttp = new MockHttpHandler();
  }

  [TearDown]
  public void UnInit()
  {
    _httpClient.Dispose();
    _mockHttp.Dispose();
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

    using var host = await builder.StartAsync();

    var client = host.GetTestClient();

    Func<Task> act = async () => await client.GetAsync("/test-exception");

    await act.Should().ThrowAsync<Exception>().WithMessage("Banana's are indeed yellow");

    var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
    
    while (!token.IsCancellationRequested && _mockHttp.InvokedRequests.Count == 0)
    {
    }

    _mockHttp.InvokedRequests.Should().HaveCount(1);
  }
}