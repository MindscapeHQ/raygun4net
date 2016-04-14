using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.DependencyInjection;

namespace Mindscape.Raygun4Net.AspNetCore
{
  public interface IRaygunSettingsProvider
  {
    RaygunSettings GetRaygunSettings(RaygunSettings baseSettings);
  }

  public class RaygunSettingsProvider : IRaygunSettingsProvider
  {
    private readonly Func<RaygunSettings, RaygunSettings> _customConfig;

    public RaygunSettingsProvider(Func<RaygunSettings, RaygunSettings> customConfig)
    {
      this._customConfig = customConfig;
    }


    public RaygunSettings GetRaygunSettings(RaygunSettings baseSettings)
    {
      if(_customConfig != null)
      {
        return _customConfig(baseSettings);
      }
      return baseSettings;
    }
  }

  public class RaygunAspNetMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunSettings _settings;

    public RaygunAspNetMiddleware(RequestDelegate next, IOptions<RaygunSettings> settings, IRaygunSettingsProvider customConfig)
    {
      _next = next;

      if(customConfig != null)
      {
        _settings = customConfig.GetRaygunSettings(settings.Value ?? new RaygunSettings());  
      }
      else
      {
        _settings = settings.Value ?? new RaygunSettings();
      }
    }
    public async Task Invoke(HttpContext httpContext)
    {
      try
      {
        await _next.Invoke(httpContext);
      }
      catch(Exception e)
      {
        var client = new RaygunAspNetCoreClient(_settings);
        client.RaygunCurrentRequest(httpContext);
        await client.SendInBackground(e);
        throw;
      }
    }
  }

  public static class IApplicationBuilderExtensions
  {
    public static IApplicationBuilder UseRaygun(this IApplicationBuilder app)
    {
      return app.UseMiddleware<RaygunAspNetMiddleware>();
    }

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, Func<RaygunSettings, RaygunSettings> customConfig = null)
    {
      var settings = configuration.GetSection("RaygunSettings");
      services.Configure<RaygunSettings>(settings);
      services.AddInstance<IRaygunSettingsProvider>(new RaygunSettingsProvider(customConfig));
      
      return services;
    }
  }
}