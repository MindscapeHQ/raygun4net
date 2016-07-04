using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
        if(_settings.ExcludeErrorsFromLocal && httpContext.Request.IsLocal())
        {
          throw;
        }

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

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfigurationRoot configuration)
    {
      services.Configure<RaygunSettings>(configuration.GetSection("RaygunSettings"));

      services.AddTransient<IRaygunAspNetCoreClientProvider>(_ => new DefaultRaygunAspNetCoreClientProvider());
      services.AddSingleton<RaygunMiddlewareSettings>();

      return services;
    }

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, RaygunMiddlewareSettings middlewareSettings)
    {
      services.Configure<RaygunSettings>(configuration.GetSection("RaygunSettings"));

      services.AddTransient(_ => middlewareSettings.ClientProvider ?? new DefaultRaygunAspNetCoreClientProvider());
      services.AddTransient(_ => middlewareSettings);

      return services;
    }
  }

  internal static class HttpRequestExtensions
  {
    /// <summary>
    /// Returns true if the IP address of the request originator was 127.0.0.1 or if the IP address of the request was the same as the server's IP address.
    /// </summary>
    /// <remarks>
    /// Credit to Filip W for the initial implementation of this method.
    /// See http://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/
    /// </remarks>
    /// <param name="req"></param>
    /// <returns></returns>
    public static bool IsLocal(this HttpRequest req)
    {
      var connection = req.HttpContext.Connection;
      if (connection.RemoteIpAddress != null)
      {
        if (connection.LocalIpAddress != null)
        {
          return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
        }

        return IPAddress.IsLoopback(connection.RemoteIpAddress);
      }

      // for in memory TestServer or when dealing with default connection info
      if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
      {
        return true;
      }

      return false;
    }
  }
}