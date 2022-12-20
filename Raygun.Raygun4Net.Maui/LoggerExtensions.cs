using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;

namespace Raygun.Raygun4Net.Maui;

public static class LoggerExtensions
{
  //public static ILoggingBuilder AddColorConsoleLogger(
  //  this ILoggingBuilder builder)
  //{
  //  // builder.AddConfiguration();

  //  builder.Services.AddOptions<RaygunSettings>();
  //  builder.Services.Configure<RaygunSettings>(c => builder.Configuration.GetSection(nameof(RaygunSettings)).Bind(c));
  //  builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ColorConsoleLoggerProvider>());

  //  //   LoggerProviderOptions.RegisterProviderOptions<ColorConsoleLoggerConfiguration, ColorConsoleLoggerProvider>(builder.Services);

  //  return builder;
  //}
  public static MauiAppBuilder AddRaygunLogger(
    this MauiAppBuilder builder)
  {
    // builder.AddConfiguration();
 
   builder.Services.AddOptions<RaygunSettings>().BindConfiguration(nameof(RaygunSettings));
    //builder.Services.Configure<RaygunSettings>(c =>
    //{

    //  builder.Configuration.GetSection(nameof(RaygunSettings));
    //});
    builder.Logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, RaygunLoggerProvider>());

      // LoggerProviderOptions.RegisterProviderOptions<RaygunSettings, RaygunLoggerProvider>(builder.Services);

    return builder;
  }

  //public static ILoggingBuilder AddColorConsoleLogger(
  //  this ILoggingBuilder builder,
  //  Action<ColorConsoleLoggerConfiguration> configure)
  //{
  //  builder.AddColorConsoleLogger();
  //  builder.Services.Configure(configure);

  //  return builder;
  //}
}

//  [UnsupportedOSPlatform("browser")]