#nullable enable

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mindscape.Raygun4Net.AspNetCore;

public static class ApplicationBuilderExtensions
{
  public static IApplicationBuilder UseRaygun(this IApplicationBuilder app)
  {
    return app.UseMiddleware<RaygunMiddleware>();
  }

  public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, Action<RaygunSettings>? configure = null)
  {
    // Fetch settings from configuration or use default settings
    var settings = configuration.GetSection("RaygunSettings").Get<RaygunSettings>() ?? new RaygunSettings();
    
    // Override settings with user-provided settings
    configure?.Invoke(settings);
    
    services.TryAddSingleton(settings);
    services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()));
    services.AddHttpContextAccessor();

    return services;
  }

  public static IServiceCollection AddRaygun(this IServiceCollection services, Action<RaygunSettings>? configure = null)
  {
    // Fetch settings from configuration or use default settings
    var settings = new RaygunSettings();
    
    // Override settings with user-provided settings
    configure?.Invoke(settings);
    
    services.TryAddSingleton(settings);
    services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()));
    services.AddHttpContextAccessor();

    return services;
  }
}