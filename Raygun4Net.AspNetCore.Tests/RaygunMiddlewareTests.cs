using System.Net;
using System.Security.Claims;
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
  private static TaskCompletionSource<bool> _notifyCompletionSource = null!;

  // ReSharper disable once ClassNeverInstantiated.Local
  private class BananaUserProvider : IRaygunUserProvider
  {
    public RaygunIdentifierMessage GetUser()
    {
      return new RaygunIdentifierMessage("Banana User")
      {
        Email = "Banana Email"
      };
    }
  }

  [SetUp]
  public async Task Init()
  {
    _mockHttp = new MockHttpHandler();
    _httpClient = new HttpClient(_mockHttp);
    _notifyCompletionSource = new TaskCompletionSource<bool>();

    var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
    {
      webBuilder.UseTestServer()
                .ConfigureServices((_, services) =>
                {
                  services.AddRouting();
                  services.AddSingleton<RaygunClient>(s => new RaygunClient(s.GetService<RaygunSettings>(), _httpClient, s.GetService<IRaygunUserProvider>()));
                  services.AddRaygun(options: settings =>
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
                    endpoints.MapGet("/user", context =>
                    {
                      var claims = new List<Claim>
                      {
                        new(ClaimTypes.Email, "banana@banana.com"),
                        new(ClaimTypes.Name, "Banana Name")
                      };
                      
                      var identity = new ClaimsIdentity(claims, "Banana Auth");
                      var claimsPrincipal = new ClaimsPrincipal(identity);
                      
                      context.User = claimsPrincipal;

                      throw new Exception("omg a fake error!");
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

  [Test]
  public async Task WhenExceptionIsThrown_ShouldBeCapturedByMiddleware_EntrySentToRaygun()
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync("/test-exception");

    await act.Should().ThrowAsync<Exception>().WithMessage("Banana's are indeed yellow");

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

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
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
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

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

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
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync("/handled");

    await act.Should().ThrowAsync<Exception>().WithMessage("I should be handled only once...");

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

    // We are verifying the Try/Catch block in the /handled endpoint sends to Raygun
    // and that the middleware did not also send the exception to Raygun. So total sends should be 1
    _mockHttp.InvokedRequests.Should().HaveCount(1);
  }

  [Test]
  public async Task WhenNoExceptionThrown_ShouldNotMessWithPayload_AndNotSendToRaygun()
  {
    var response = await _client.GetAsync("/");

    response.IsSuccessStatusCode.Should().BeTrue();
    response.Content.ReadAsStringAsync().Result.Should().Be("Hello World!");

    _mockHttp.InvokedRequests.Should().BeEmpty();
  }

  [Test]
  public async Task WhenExceptionIsThrown_CustomUserInfoShouldBePopulated()
  {
    // Test re-init with overridden IRaygunUserProvider
    var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
    {
      webBuilder.UseTestServer()
                .ConfigureServices((_, services) =>
                {
                  services.AddRouting();
                  services.AddSingleton<RaygunClient>(s => new RaygunClient(s.GetService<RaygunSettings>(), _httpClient, s.GetService<IRaygunUserProvider>()));
                  services.AddRaygun(options: settings => { settings.ApiKey = "banana"; });
                  services.AddRaygunUserProvider<BananaUserProvider>();
                })
                .Configure(app =>
                {
                  app.UseRouting();
                  app.UseRaygun();
                  app.UseEndpoints(endpoints => { endpoints.MapGet("/test-exception", new Func<object>(() => throw new Exception("Banana's are indeed yellow"))); });
                });
    });

    var host = await builder.StartAsync();
    var client = host.GetTestClient();

    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
             }).Verifiable();

    Func<Task> act = async () => await client.GetAsync("/test-exception");
    await act.Should().ThrowAsync<Exception>();

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

    var request = _mockHttp.InvokedRequests[0].Request;
    var content = await request.Content?.ReadAsStringAsync()!;

    var raygunMsg = JsonSerializer.Deserialize<RaygunMessage>(content)!;

    raygunMsg.Details.User.Identifier.Should().Be("Banana User");
    raygunMsg.Details.User.Email.Should().Be("Banana Email");
  }

  [Test]
  public async Task WhenExceptionIsThrown_AndUserIsLoggedIn_ShouldFetchUserUsingDefaultProvider()
  {
    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync("/user");
    await act.Should().ThrowAsync<Exception>();

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

    var request = _mockHttp.InvokedRequests[0].Request;
    var content = await request.Content?.ReadAsStringAsync()!;

    var raygunMsg = JsonSerializer.Deserialize<RaygunMessage>(content)!;

    raygunMsg.Details.User.Identifier.Should().Be("banana@banana.com");
    raygunMsg.Details.User.Email.Should().Be("banana@banana.com");
    raygunMsg.Details.User.FullName.Should().Be("Banana Name");
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
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
             }).Verifiable();

    Func<Task> act = async () => await _client.GetAsync($"/{(int)statusCode}");

    await act.Should().ThrowAsync<Exception>().WithMessage(expectedContent);

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

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
                  services.AddRaygun(options: settings =>
                  {
                    settings.ApiKey = "banana";
                    settings.ExcludeErrorsFromLocal = true;
                  });
                })
                .Configure(app =>
                {
                  app.UseRouting();
                  app.UseRaygun();
                  app.UseEndpoints(endpoints => { endpoints.MapGet("/test-exception", new Func<object>(() => throw new Exception("Banana's are indeed yellow"))); });
                });
    });

    var host = await builder.StartAsync();
    var client = host.GetTestClient();

    _mockHttp.When(match => match.Method(HttpMethod.Post).RequestUri("https://api.raygun.com/entries"))
             .Respond(x =>
             {
               x.Body("OK");
               x.StatusCode(HttpStatusCode.Accepted);
               _ = Task.Delay(TimeSpan.FromMilliseconds(1000)).ContinueWith(_ => _notifyCompletionSource.SetResult(true));
             }).Verifiable();

    Func<Task> act = async () => await client.GetAsync($"/test-exception");

    await act.Should().ThrowAsync<Exception>();

    await _notifyCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

    _mockHttp.InvokedRequests.Should().BeEmpty();
  }
}