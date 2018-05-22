using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Mindscape.Raygun4Net.AspNetCore.Builders
{
  public class RaygunAspNetMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunMiddlewareSettings _middlewareSettings;
    private readonly AspNetCore.RaygunSettings _settings;

    public RaygunAspNetMiddleware(RequestDelegate next, IOptions<AspNetCore.RaygunSettings> settings, RaygunMiddlewareSettings middlewareSettings)
    {
      _next = next;
      _middlewareSettings = middlewareSettings;

      _settings = _middlewareSettings.ClientProvider.GetRaygunSettings(settings.Value ?? new AspNetCore.RaygunSettings());
    }
    public async Task Invoke(HttpContext httpContext)
    {
      MemoryStream buffer = null;
      Stream originalRequestBody = null;

      if (_settings.ReplaceUnseekableRequestStreams)
      {
        try
        {
          var contentType = httpContext.Request.ContentType;
          //ignore conditions
          var streamIsNull = httpContext.Request.Body == Stream.Null;
          var streamIsRewindable = httpContext.Request.Body.CanSeek;
          var isFormUrlEncoded = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded", CompareOptions.IgnoreCase) >= 0;
          var isTextHtml = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "text/html", CompareOptions.IgnoreCase) >= 0;
          var isHttpGet = httpContext.Request.Method == "GET"; //should be no request body to be concerned with

          //if any of the ignore conditions apply, don't modify the Body Stream
          if (!(streamIsNull || isFormUrlEncoded || streamIsRewindable || isTextHtml || isHttpGet))
          {
            //copy, rewind and replace the stream
            buffer = new MemoryStream();
            originalRequestBody = httpContext.Request.Body;

            await originalRequestBody.CopyToAsync(buffer);
            buffer.Seek(0, SeekOrigin.Begin);

            httpContext.Request.Body = buffer;
          }
        }
        catch (Exception e)
        {
          Debug.WriteLine(string.Format("Error replacing request stream {0}", e.Message));

          if (_settings.ThrowOnError)
          {
            throw;
          }
        }
      }
 
      try
      {
        await _next.Invoke(httpContext);
      }
      catch (Exception e)
      {
        if (_settings.ExcludeErrorsFromLocal && httpContext.Request.IsLocal())
        {
          throw;
        }

        var client = _middlewareSettings.ClientProvider.GetClient(_settings, httpContext);
        await client.SendInBackground(e);
        throw;
      }
      finally
      {
        buffer?.Dispose();
        if (originalRequestBody != null)
        {
          httpContext.Request.Body = originalRequestBody;
        }
      }
    }
  }

  public static class ApplicationBuilderExtensions
  {
    public static IApplicationBuilder UseRaygun(this IApplicationBuilder app)
    {
      return app.UseMiddleware<RaygunAspNetMiddleware>();
    }

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfigurationRoot configuration)
    {
      services.Configure<AspNetCore.RaygunSettings>(configuration.GetSection("RaygunSettings"));

      services.AddTransient<IRaygunAspNetCoreClientProvider>(_ => new DefaultRaygunAspNetCoreClientProvider());
      services.AddSingleton<RaygunMiddlewareSettings>();

      return services;
    }

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, RaygunMiddlewareSettings middlewareSettings)
    {
      services.Configure<AspNetCore.RaygunSettings>(configuration.GetSection("RaygunSettings"));

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
        return (connection.LocalIpAddress != null && connection.RemoteIpAddress.Equals(connection.LocalIpAddress)) || IPAddress.IsLoopback(connection.RemoteIpAddress);
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