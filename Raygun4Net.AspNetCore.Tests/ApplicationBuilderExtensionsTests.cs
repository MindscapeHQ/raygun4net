using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net.Extensions.Logging;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

// A user provider or message builder that depends on ILogger made a dependency cycle with the Raygun logger:
// client -> user provider -> ILogger -> RaygunLoggerProvider -> client. Resolution stopped and did not throw.
[TestFixture]
public class ApplicationBuilderExtensionsTests
{
  private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

  [Test]
  public void ResolvingLogger_Completes_WhenUserProviderDependsOnLogger()
  {
    var services = BuildServices(s => s.AddRaygun(options => options.ApiKey = "banana"), s => s.AddRaygunUserProvider<LoggingUserProvider>());

    Completes(() => services.GetRequiredService<ILogger<ApplicationBuilderExtensionsTests>>()).Should().BeTrue();
  }

  [Test]
  public void ResolvingLogger_Completes_WhenUserProviderDependsOnLoggerAndSettingsComeFromConfiguration()
  {
    var configuration = new ConfigurationBuilder().Build();
    var services = BuildServices(s => s.AddRaygun(configuration), s => s.AddRaygunUserProvider<LoggingUserProvider>());

    Completes(() => services.GetRequiredService<ILogger<ApplicationBuilderExtensionsTests>>()).Should().BeTrue();
  }

  [Test]
  public async Task SendInBackground_RunsRequestDataBuilderAndMessageBuilders_WhenTheyDependOnLogger()
  {
    var services = BuildServices(s => s.AddRaygun(options => options.ApiKey = "banana"), s => s.AddSingleton<IMessageBuilder, RecordingMessageBuilder>());

    RaygunClientBase? client = null;

    Completes(() => client = services.GetRequiredService<RaygunClientBase>()).Should().BeTrue();

    await client!.SendInBackground(new Exception("test"));

    // AddRaygun registers RequestDataBuilder before the test's message builder, so it sets the request first
    var builder = services.GetServices<IMessageBuilder>().OfType<RecordingMessageBuilder>().Single();
    builder.HadRequest.Should().Equal(true);
  }

  private static ServiceProvider BuildServices(Action<IServiceCollection> addRaygun, Action<IServiceCollection> configure)
  {
    var services = new ServiceCollection();

    addRaygun(services);
    services.AddLogging(logging => logging.AddRaygunLogger());
    configure(services);

    return services.BuildServiceProvider();
  }

  // Runs the action on another thread, so that a dependency cycle fails the test instead of hanging the test run
  private static bool Completes(Action action)
  {
    return Task.Run(action).Wait(Timeout);
  }

  // ReSharper disable once ClassNeverInstantiated.Local
  private class LoggingUserProvider : IRaygunUserProvider
  {
    public LoggingUserProvider(ILogger<LoggingUserProvider> logger)
    {
    }

    public RaygunIdentifierMessage? GetUser()
    {
      return null;
    }
  }

  // ReSharper disable once ClassNeverInstantiated.Local
  private class RecordingMessageBuilder : IMessageBuilder
  {
    private readonly List<bool> _hadRequest = new();

    public RecordingMessageBuilder(ILogger<RecordingMessageBuilder> logger)
    {
    }

    public IReadOnlyList<bool> HadRequest
    {
      get
      {
        lock (_hadRequest)
        {
          return _hadRequest.ToArray();
        }
      }
    }

    public Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
    {
      lock (_hadRequest)
      {
        _hadRequest.Add(message.Details.Request != null);
      }

      return Task.FromResult(message);
    }
  }
}
