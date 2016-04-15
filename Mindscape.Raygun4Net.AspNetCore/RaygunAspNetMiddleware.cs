using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.DependencyInjection;

namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunAspNetMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunMiddlewareSettings _middlewareSettings;
    private readonly RaygunSettings _settings;

    public RaygunAspNetMiddleware(RequestDelegate next, IOptions<RaygunSettings> settings, RaygunMiddlewareSettings middlewareSettings)
    {
      _next = next;
      _middlewareSettings = middlewareSettings;

      _settings = _middlewareSettings.ClientProvider.GetRaygunSettings(settings.Value ?? new RaygunSettings());  
    }
    public async Task Invoke(HttpContext httpContext)
    {
      try
      {
        await _next.Invoke(httpContext);
      }
      catch(Exception e)
      {
        /*
         * There is no IsLocal on IPAddress or anything, so this is way harder than it looks. 
         * https://github.com/aspnet/Hosting/issues/570#issuecomment-171571555
         * https://github.com/aspnet/HttpAbstractions/issues/536
        if(_settings.ExcludeErrorsFromLocal && IS_LOCAL_CONNECTION)
        {
          throw;
        }*/

        var client = _middlewareSettings.ClientProvider.GetClient(_settings);
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

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration)
    {
      var settings = configuration.GetSection("RaygunSettings");
      services.Configure<RaygunSettings>(settings);
      services.AddInstance<IRaygunAspNetCoreClientProvider>(new DefaultRaygunAspNetCoreClientProvider());
      services.AddSingleton<RaygunMiddlewareSettings>();

      return services;
    }

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, RaygunMiddlewareSettings middlewareSettings)
    {
      var settings = configuration.GetSection("RaygunSettings");
      services.Configure<RaygunSettings>(settings);

      services.AddInstance(middlewareSettings.ClientProvider ?? new DefaultRaygunAspNetCoreClientProvider());
      services.AddInstance(middlewareSettings);

      return services;
    }
  }
}