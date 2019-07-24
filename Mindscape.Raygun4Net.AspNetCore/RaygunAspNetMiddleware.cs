using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mindscape.Raygun4Net.ProfilingSupport;

namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunAspNetMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunMiddlewareSettings _middlewareSettings;
    private readonly RaygunSettings _settings;

    private static TimeSpan AgentPollingDelay = new TimeSpan(0, 5, 0);
    private static ISamplingManager _samplingManager;

    public RaygunAspNetMiddleware(RequestDelegate next, IOptions<RaygunSettings> settings, RaygunMiddlewareSettings middlewareSettings)
    {
      _next = next;
      _middlewareSettings = middlewareSettings;

      _settings = _middlewareSettings.ClientProvider.GetRaygunSettings(settings.Value ?? new RaygunSettings());

      InitProfilingSupport();
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
        if (_samplingManager != null)
        {
          var request = httpContext.Request;
          Uri uri;
          if (request.QueryString.HasValue)
            uri = new Uri($"{request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}");
          else
            uri = new Uri($"{request.Scheme}://{request.Host}{request.Path}");

          if (!_samplingManager.TakeSample(uri))
          {
            APM.Disable();
          }
        }

        await _next.Invoke(httpContext);

        if (_samplingManager != null)
        {
          APM.Enable();
        }
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

    private void InitProfilingSupport()
    {
      if (APM.ProfilerAttached)
      {
        _samplingManager = new SamplingManager();

        // See http://blog.i3arnon.com/2015/07/02/task-run-long-running/ for why TaskCreationOptions.LongRunning is needed
        _settingsTask = new Task(() => RefreshAgentSettings(), TaskCreationOptions.LongRunning);
        _settingsTask.Start();
      }
    }

    private static string settingsFilePath = Path.Combine(
#if NETSTANDARD1_6
        @"C:\ProgramData\Raygun\AgentSettings\", // the best we can do under .NET Std 1.6|
#else
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
#endif
        "Raygun", "AgentSettings", "agent-configuration.json");

    private void RefreshAgentSettings()
    {
      while (true)
      {
        try
        {
          if (File.Exists(settingsFilePath))
          {
            var settingsText = File.ReadAllText(settingsFilePath);
            // In .NET Core, siteName needs to be the the name of the main dll, i.e. MyApp.dll
            var siteName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            var samplingSetting = SettingsManager.ParseSamplingSettings(settingsText, siteName);
            if (samplingSetting != null)
              _samplingManager.SetSamplingPolicy(samplingSetting.Policy, samplingSetting.Overrides);
          }
        }
#if NETSTANDARD2_0
        catch (ThreadAbortException /*threadEx*/)
        {
          return;
        }
#endif
        catch (Exception /*ex*/)
        {
        }

        Task.Delay(AgentPollingDelay).Wait();
      }
    }
  }

  public static class ApplicationBuilderExtensions
  {
    public static IApplicationBuilder UseRaygun(this IApplicationBuilder app)
    {
      return app.UseMiddleware<RaygunAspNetMiddleware>();
    }

    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration)
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