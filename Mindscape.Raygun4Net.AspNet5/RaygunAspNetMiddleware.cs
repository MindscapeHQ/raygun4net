using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.DependencyInjection;

namespace Mindscape.Raygun4Net.AspNet5
{
  public class RaygunAspNetMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunSettings _settings;

    public RaygunAspNetMiddleware(RequestDelegate next, IOptions<RaygunSettings> settings)
    {
      _next = next;
      _settings = settings.Value;
    }
    public Task Invoke(HttpContext httpContext)
    {
      return _next.Invoke(httpContext).ContinueWith(appState =>
      {
        if (appState.IsFaulted && appState.Exception != null)
        {

        }
      });
    }
  }

  public static class IApplicationBuilderExtensions
  {
    public static IApplicationBuilder AddRaygun(this IApplicationBuilder app, Action<RaygunSettings> customConfig = null)
    {
      return app.UseMiddleware<RaygunAspNetMiddleware>();
    }

    public static IServiceCollection LoadRaygunConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
      var settings = configuration.GetSection("RaygunSettings");
      services.Configure<RaygunSettings>(settings);
      return services;
    }
  }
}