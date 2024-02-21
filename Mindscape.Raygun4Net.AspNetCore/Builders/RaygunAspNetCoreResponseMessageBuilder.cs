#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Mindscape.Raygun4Net.AspNetCore.Builders
{
  internal class RaygunAspNetCoreResponseMessageBuilder
  {
    public static RaygunResponseMessage Build(HttpContext? context)
    {
      if (context == null)
      {
        return new RaygunResponseMessage();
      }
      
      var httpResponseFeature = context.Features.Get<IHttpResponseFeature>();
      return new RaygunResponseMessage
      {
        StatusCode = context.Response.StatusCode,
        StatusDescription = httpResponseFeature?.ReasonPhrase
      };
    }
  }
}