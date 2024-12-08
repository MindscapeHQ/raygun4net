#nullable enable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Mindscape.Raygun4Net.AspNetCore.Builders
{
  // ReSharper disable once ClassNeverInstantiated.Global
  public class RaygunAspNetCoreResponseMessageBuilder
  {
    public static Task<RaygunResponseMessage> Build(HttpContext? context, RaygunSettings _)
    {
      if (context == null)
      {
        return Task.FromResult(new RaygunResponseMessage());
      }

      var httpResponseFeature = context.Features.Get<IHttpResponseFeature>();
      return Task.FromResult(new RaygunResponseMessage
      {
        StatusCode = context.Response.StatusCode,
        StatusDescription = httpResponseFeature?.ReasonPhrase
      });
    }
  }
}