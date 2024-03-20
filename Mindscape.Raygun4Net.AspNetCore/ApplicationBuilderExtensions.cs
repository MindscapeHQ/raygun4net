#nullable enable

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.AspNetCore;

public static class ApplicationBuilderExtensions
{
  private const string NoApiKeyWarning = "Raygun API Key is not set, please set an API Key in the RaygunSettings.";
  
  /// <summary>
  /// Checks to see if you have an API Key and registers the Raygun Middleware. If no API Key is found, a warning will be logged.
  /// </summary>
  public static IApplicationBuilder UseRaygun(this IApplicationBuilder app)
  {
    var settings = app.ApplicationServices.GetService<RaygunSettings>();
    
    if (settings?.ApiKey == null)
    {
      var logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger<RaygunMiddleware>();

      if (logger != null)
      {
        logger.LogWarning(NoApiKeyWarning);
      }
      else
      {
        Console.WriteLine(NoApiKeyWarning);
      }
    }
    
    return app.UseMiddleware<RaygunMiddleware>();
  }

  /// <summary>
  /// Registers the Raygun Client and Raygun Settings with the DI container. Settings will be fetched from the appsettings.json file,
  /// and can be overridden by providing a custom configuration delegate.
  /// </summary>
  public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, Action<RaygunSettings>? options = null)
  {
    // Fetch settings from configuration or use default settings
    var settings = configuration.GetSection("RaygunSettings").Get<RaygunSettings>() ?? new RaygunSettings();
    
    // Override settings with user-provided settings
    options?.Invoke(settings);

    services.TryAddSingleton(settings);
    services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()!, s.GetService<IRaygunUserProvider>()!));
    services.AddHttpContextAccessor();

    return services;
  }

  /// <summary>
  /// Registers the Raygun Client and Raygun Settings with the DI container. Settings will be defaulted and overridden by providing a custom configuration delegate.
  /// </summary>
  public static IServiceCollection AddRaygun(this IServiceCollection services, Action<RaygunSettings>? options)
  {
    // Since we are not using IConfiguration, we need to create a new instance of RaygunSettings
    var settings = new RaygunSettings();
    
    // Override settings with user-provided settings
    options?.Invoke(settings);
    
    services.TryAddSingleton(settings);
    services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()!, s.GetService<IRaygunUserProvider>()!));
    services.AddHttpContextAccessor();

    return services;
  }
  
  /// <summary>
  /// Registers the default User Provider with the DI container. This will use the IHttpContextAccessor to fetch the current user.
  /// </summary>
  /// <remarks>
  /// This will attempt to check if a user is Authenticated and use the Name/Email from the claims to create a RaygunIdentifierMessage.
  /// If you wish to provide your own implementation of IRaygunUserProvider, you can use the <see cref="AddRaygunUserProvider&lt;T&gt;" /> method.
  /// </remarks>
  public static IServiceCollection AddRaygunUserProvider(this IServiceCollection services)
  {
    services.TryAddSingleton<IRaygunUserProvider, DefaultRaygunUserProvider>();
    
    return services;
  }
  
  /// <summary>
  /// Registers a custom User Provider with the DI container. This allows you to provide your own implementation of IRaygunUserProvider.
  /// </summary>
  /// <remarks>
  /// Refer to the <see cref="DefaultRaygunUserProvider" /> for an example of how to implement IRaygunUserProvider.
  /// </remarks>
  public static IServiceCollection AddRaygunUserProvider<T>(this IServiceCollection services) where T : class, IRaygunUserProvider
  {
    // In case the default or any other user provider is already registered, remove it first
    var existing = services.FirstOrDefault(x => x.ServiceType == typeof(IRaygunUserProvider));
    
    if (existing != null)
    {
      services.Remove(existing);
    }
    
    // Add the new user provider
    services.TryAddSingleton<IRaygunUserProvider, T>();
    
    return services;
  }
}