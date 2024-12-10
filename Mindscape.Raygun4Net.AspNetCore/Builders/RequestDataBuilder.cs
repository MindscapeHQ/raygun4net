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

    message.Details.Request = await RaygunAspNetCoreRequestMessageBuilder.Build(ctx, _settings);
    message.Details.Response = await RaygunAspNetCoreResponseMessageBuilder.Build(ctx, _settings);

    return message;
  }
}