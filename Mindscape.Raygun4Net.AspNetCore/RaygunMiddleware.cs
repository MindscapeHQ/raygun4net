#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunMiddleware
{
  private readonly RequestDelegate _next;
  private readonly RaygunSettings _settings;
  private readonly RaygunClient _client;
  private readonly IHttpContextAccessor _httpContextAccessor;

  private const string UnhandledExceptionTag = "UnhandledException";

  public RaygunMiddleware(RequestDelegate next,
                          IOptions<RaygunSettings> settings,
                          RaygunClient raygunClient,
                          IHttpContextAccessor httpContextAccessor)
  {
    _next = next;
    _settings = settings.Value ?? new RaygunSettings();
    _client = raygunClient;
    _httpContextAccessor = httpContextAccessor;
  }

  public async Task Invoke(HttpContext httpContext)
  {
    httpContext.Request.EnableBuffering();

    try
    {
      // Let the request get invoked as normal
      await _next.Invoke(httpContext);
    }
    catch (Exception e)
    {
      // If an exception was captured but we exclude the capture in local then just throw the exception
      if (_settings.ExcludeErrorsFromLocal && httpContext.Request.IsLocal())
      {
        throw;
      }

      // Capture the exception and send it to Raygun
      await _client.SendInBackground(e, new List<string> { UnhandledExceptionTag }, _httpContextAccessor.HttpContext);
      throw;
    }
  }
}