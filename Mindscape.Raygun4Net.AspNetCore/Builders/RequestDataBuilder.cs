using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net.AspNetCore.Builders;

public class RequestDataBuilder : IMessageBuilder
{
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly RaygunSettings _settings;

  public RequestDataBuilder(IHttpContextAccessor httpContextAccessor, RaygunSettings settings)
  {
    _httpContextAccessor = httpContextAccessor;
    _settings = settings;
  }

  public async Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
  {
    var ctx = _httpContextAccessor.HttpContext;

    // Not sent during a request, so there is no request data to add
    if (ctx == null)
    {
      return message;
    }

    // Keep request data the caller already set, for example SendInBackground(exception, tags, context)
    message.Details.Request ??= await RaygunAspNetCoreRequestMessageBuilder.Build(ctx, _settings);
    message.Details.Response ??= await RaygunAspNetCoreResponseMessageBuilder.Build(ctx, _settings);

    return message;
  }
}