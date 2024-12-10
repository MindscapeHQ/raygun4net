#nullable enable

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mindscape.Raygun4Net.NetCore;

public static class ApplicationBuilderExtensions
{
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
    services.TryAddSingleton<RaygunClientBase>(s => new RaygunClient(s.GetRequiredService<RaygunSettings>(), s.GetRequiredService<IRaygunUserProvider>(), s.GetServices<IMessageBuilder>()));

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
    services.TryAddSingleton<RaygunClientBase>(s => new RaygunClient(s.GetRequiredService<RaygunSettings>(), s.GetRequiredService<IRaygunUserProvider>(), s.GetServices<IMessageBuilder>()));

    return services;
  }
  
  /// <summary>
  /// Registers a custom User Provider with the DI container. This allows you to provide your own implementation of IRaygunUserProvider.
  /// </summary>
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