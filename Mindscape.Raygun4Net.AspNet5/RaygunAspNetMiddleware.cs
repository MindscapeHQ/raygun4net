using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using System;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.AspNet5
{
  public class RaygunAspNetMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunSettings _settings;

    public RaygunAspNetMiddleware(RequestDelegate next, RaygunSettings settings)
    {
      _next = next;
      _settings = settings;
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
    public static IApplicationBuilder AddRaygun(this IApplicationBuilder app, IConfiguration config, Action<RaygunSettings> configAction = null)
    {
      var settings = config.Get<RaygunSettings>("RaygunSettings");
      if(configAction != null)
      {
        configAction(settings);
      }
      return app.UseMiddleware<RaygunAspNetMiddleware>(settings);
    }
  }
}