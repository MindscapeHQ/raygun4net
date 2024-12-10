using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

public static class RaygunLoggerExtensions
{
  /// <summary>
  /// Adds Raygun logging capabilities to the logging builder with support for configuration and custom settings.
  /// </summary>
  /// <param name="builder">The ILoggingBuilder instance to add Raygun logging to.</param>
  /// <param name="configuration">The configuration instance containing Raygun settings under the "RaygunSettings" section.</param>
  /// <param name="options">Optional delegate to configure additional Raygun logger settings programmatically.</param>
  /// <returns>The ILoggingBuilder instance for method chaining.</returns>
  /// <remarks>
  /// This method configures Raygun logging with the following precedence:<br/>
  /// 1. Default settings<br/>
  /// 2. Configuration from the "RaygunSettings" section (if provided)<br/>
  /// 3. Programmatic options (if provided)<br/>
  /// <br/>
  /// Example usage:<br/>
  /// <code>
  /// builder.AddRaygunLogger(configuration, options =>
  /// {
  ///     options.MinimumLogLevel = LogLevel.Information;
  ///     options.OnlyLogExceptions = false;
  /// });
  /// </code>
  /// </remarks>
  public static ILoggingBuilder AddRaygunLogger(this ILoggingBuilder builder, IConfiguration? configuration, Action<RaygunLoggerSettings>? options = null)
  {
    // Since we are not using IConfiguration, we need to create a new instance of RaygunSettings
    var settings = new RaygunLoggerSettings();

    // Fetch settings from configuration or use default settings
    configuration?.GetSection("RaygunSettings").Bind(settings);

    // Override settings with user-provided settings
    options?.Invoke(settings);

    builder.Services.TryAddSingleton(settings);
    builder.Services.AddSingleton<ILoggerProvider, RaygunLoggerProvider>();

    return builder;
  }
  
  /// <summary>
  /// Adds Raygun logging capabilities to the logging builder with programmatic configuration only.
  /// </summary>
  /// <param name="builder">The ILoggingBuilder instance to add Raygun logging to.</param>
  /// <param name="options">Optional delegate to configure Raygun logger settings programmatically.</param>
  /// <returns>The ILoggingBuilder instance for method chaining.</returns>
  /// <remarks>
  /// This overload is useful when you want to configure Raygun logging entirely through code without using an IConfiguration instance.<br/>
  /// <br/>
  /// Example usage:<br/>
  /// <code>
  /// builder.AddRaygunLogger(options =>
  /// {
  ///     options.MinimumLogLevel = LogLevel.Information;
  ///     options.OnlyLogExceptions = false;
  /// });
  /// </code>
  /// </remarks>
  public static ILoggingBuilder AddRaygunLogger(this ILoggingBuilder builder, Action<RaygunLoggerSettings>? options = null)
  {
    // Since we are not using IConfiguration, we need to create a new instance of RaygunSettings
    var settings = new RaygunLoggerSettings();

    // Override settings with user-provided settings
    options?.Invoke(settings);

    builder.Services.TryAddSingleton(settings);
    builder.Services.AddSingleton<ILoggerProvider, RaygunLoggerProvider>();

    return builder;
  }
}