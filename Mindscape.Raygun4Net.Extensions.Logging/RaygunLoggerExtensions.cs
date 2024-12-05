using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

public static class RaygunLoggerExtensions
{
  public static ILoggingBuilder AddRaygunLogger(this ILoggingBuilder builder, IConfiguration? configuration = null, Action<RaygunLoggerSettings>? options = null)
  {
    // Since we are not using IConfiguration, we need to create a new instance of RaygunSettings
    var settings = new RaygunLoggerSettings();

    // Fetch settings from configuration or use default settings
    configuration?.GetSection("RaygunSettings").Bind(settings);

    // Override settings with user-provided settings
    options?.Invoke(settings);

    builder.Services.TryAddSingleton(settings);

    //builder.Services.TryAddSingleton<RaygunClientBase>(s => new RaygunClientBase(s.GetService<RaygunSettingsBase>()!, s.GetService<IRaygunUserProvider>()!));
    builder.Services.AddSingleton<ILoggerProvider, RaygunLoggerProvider>();

    return builder;
  }
}

public class RaygunLoggerSettings
{
  public LogLevel LogLevel { get; set; } = LogLevel.Error;
}