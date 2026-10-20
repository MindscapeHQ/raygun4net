using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net.Extensions.Logging;
using Mindscape.Raygun4Net.Offline;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  // A user provider or message builder that depends on ILogger made a dependency cycle with the Raygun logger:
  // client -> user provider -> ILogger -> RaygunLoggerProvider -> client. Resolution stopped and did not throw.
  [TestFixture]
  public class ApplicationBuilderExtensionsTests
  {
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    public void ResolvingLogger_CreatesClient_WhenUserProviderDependsOnLogger()
    {
      var store = new CallbackRecordingOfflineStore();
      var services = BuildServices(store, s => s.AddRaygunUserProvider<LoggingUserProvider>());

      Assert.That(Completes(() => services.GetRequiredService<ILogger<ApplicationBuilderExtensionsTests>>()), Is.True);
      Assert.That(store.IsSendCallbackSet, Is.True, "The client was not created when the logger was resolved");
    }

    [Test]
    public void ResolvingClient_Completes_WhenUserProviderDependsOnLogger()
    {
      var services = BuildServices(new CallbackRecordingOfflineStore(), s => s.AddRaygunUserProvider<LoggingUserProvider>());

      Assert.That(Completes(() => services.GetRequiredService<RaygunClientBase>()), Is.True);
    }

    [Test]
    public void ResolvingLogger_Completes_WhenMessageBuilderDependsOnLogger()
    {
      var services = BuildServices(new CallbackRecordingOfflineStore(), s => s.AddSingleton<IMessageBuilder, RecordingMessageBuilder>());

      Assert.That(Completes(() => services.GetRequiredService<ILogger<ApplicationBuilderExtensionsTests>>()), Is.True);
    }

    [Test]
    public void ResolvingLogger_Completes_WhenOwnClientFactoryUsesLazyUserProviderAndMessageBuilders()
    {
      var services = new ServiceCollection();

      // A client factory of your own is the only way to give the client, for example, its own HttpClient
      services.AddSingleton<RaygunClientBase>(s => new RaygunClient(new RaygunSettings(), new LazyRaygunUserProvider(() => s.GetService<IRaygunUserProvider>()), new LazyMessageBuilders(() => s.GetServices<IMessageBuilder>())));
      services.AddLogging(logging => logging.AddRaygunLogger());
      services.AddRaygunUserProvider<LoggingUserProvider>();
      services.AddSingleton<IMessageBuilder, RecordingMessageBuilder>();

      var provider = services.BuildServiceProvider();

      Assert.That(Completes(() => provider.GetRequiredService<ILogger<ApplicationBuilderExtensionsTests>>()), Is.True);
    }

    [Test]
    public async Task SendInBackground_UsesUserProviderAndMessageBuilders_WhenTheyDependOnLogger()
    {
      var services = BuildServices(new CallbackRecordingOfflineStore(), s =>
      {
        s.AddRaygunUserProvider<LoggingUserProvider>();
        s.AddSingleton<IMessageBuilder, RecordingMessageBuilder>();
      });

      RaygunClientBase client = null;

      Assert.That(Completes(() => client = services.GetRequiredService<RaygunClientBase>()), Is.True);

      await client.SendInBackground(new Exception("test"));

      var builder = (RecordingMessageBuilder)services.GetRequiredService<IMessageBuilder>();
      Assert.That(builder.UserIdentifiers, Is.EqualTo(new[] { LoggingUserProvider.Identifier }));
    }

    [Test]
    public void LogError_SendsTheConstructorLog_WhenUserProviderLogsAnErrorInItsConstructor()
    {
      var services = BuildServices(new CallbackRecordingOfflineStore(), s =>
      {
        s.AddSingleton<ConstructorCounter>();
        s.AddRaygunUserProvider<ErrorLoggingUserProvider>();
        s.AddSingleton<IMessageBuilder, RecordingMessageBuilder>();
      });

      Assert.That(Completes(() =>
      {
        var logger = services.GetRequiredService<ILogger<ApplicationBuilderExtensionsTests>>();
        logger.LogError(new Exception("test"), "test");
      }), Is.True);
      Assert.That(services.GetRequiredService<ConstructorCounter>().Count, Is.EqualTo(1));

      // The log from the constructor is sent without a user, then the test log
      var builder = (RecordingMessageBuilder)services.GetRequiredService<IMessageBuilder>();
      Assert.That(builder.UserIdentifiers, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SendInBackground_SendsWithoutUser_WhenUserProviderCannotBeCreated()
    {
      var services = BuildServices(new CallbackRecordingOfflineStore(), s =>
      {
        s.AddRaygunUserProvider<ThrowingUserProvider>();
        s.AddSingleton<IMessageBuilder, RecordingMessageBuilder>();
      });

      var client = services.GetRequiredService<RaygunClientBase>();

      await client.SendInBackground(new Exception("test"));

      var builder = (RecordingMessageBuilder)services.GetRequiredService<IMessageBuilder>();
      Assert.That(builder.UserIdentifiers, Is.EqualTo(new string[] { null }));
    }

    [Test]
    public void SendInBackground_SendsWithoutMessageBuilders_WhenCreatingThemFails()
    {
      var services = BuildServices(new CallbackRecordingOfflineStore(), s => s.AddSingleton<IMessageBuilder, ThrowingMessageBuilder>());

      var client = services.GetRequiredService<RaygunClientBase>();

      Assert.DoesNotThrowAsync(() => client.SendInBackground(new Exception("test")));
    }

    private static ServiceProvider BuildServices(OfflineStoreBase store, Action<IServiceCollection> configure)
    {
      var services = new ServiceCollection();

      services.AddRaygun(options => options.OfflineStore = store);
      services.AddLogging(logging => logging.AddRaygunLogger(options => options.OnlyLogExceptions = false));
      configure(services);

      return services.BuildServiceProvider();
    }

    // Runs the action on another thread, so that a dependency cycle fails the test instead of hanging the test run
    private static bool Completes(Action action)
    {
      return Task.Run(action).Wait(Timeout);
    }

    private class LoggingUserProvider : IRaygunUserProvider
    {
      public const string Identifier = "test-user";

      public LoggingUserProvider(ILogger<LoggingUserProvider> logger)
      {
      }

      public RaygunIdentifierMessage GetUser()
      {
        return new RaygunIdentifierMessage(Identifier);
      }
    }

    private class ErrorLoggingUserProvider : IRaygunUserProvider
    {
      public ErrorLoggingUserProvider(ILogger<ErrorLoggingUserProvider> logger, ConstructorCounter counter)
      {
        counter.Increment();
        logger.LogError(new Exception("constructor"), "constructor");
      }

      public RaygunIdentifierMessage GetUser()
      {
        return null;
      }
    }

    private class ThrowingUserProvider : IRaygunUserProvider
    {
      public ThrowingUserProvider()
      {
        throw new InvalidOperationException("The user provider cannot be created");
      }

      public RaygunIdentifierMessage GetUser()
      {
        return null;
      }
    }

    private class ThrowingMessageBuilder : IMessageBuilder
    {
      public ThrowingMessageBuilder()
      {
        throw new InvalidOperationException("The message builder cannot be created");
      }

      public Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
      {
        return Task.FromResult(message);
      }
    }

    private class ConstructorCounter
    {
      private int _count;

      public int Count => _count;

      public int Increment()
      {
        return Interlocked.Increment(ref _count);
      }
    }

    private class RecordingMessageBuilder : IMessageBuilder
    {
      private readonly List<string> _userIdentifiers = new();

      public RecordingMessageBuilder(ILogger<RecordingMessageBuilder> logger)
      {
      }

      public IReadOnlyList<string> UserIdentifiers
      {
        get
        {
          lock (_userIdentifiers)
          {
            return _userIdentifiers.ToArray();
          }
        }
      }

      public Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
      {
        lock (_userIdentifiers)
        {
          _userIdentifiers.Add(message.Details.User?.Identifier);
        }

        return Task.FromResult(message);
      }
    }

    private class CallbackRecordingOfflineStore : OfflineStoreBase
    {
      public CallbackRecordingOfflineStore() : base(new NoOpSendStrategy())
      {
      }

      public bool IsSendCallbackSet { get; private set; }

      public override void SetSendCallback(SendHandler sendHandler)
      {
        IsSendCallbackSet = true;
        base.SetSendCallback(sendHandler);
      }

      public override Task<List<CrashReportStoreEntry>> GetAll(CancellationToken cancellationToken)
      {
        return Task.FromResult(new List<CrashReportStoreEntry>());
      }

      public override Task<bool> Save(string crashPayload, string apiKey, CancellationToken cancellationToken)
      {
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
  }
}
