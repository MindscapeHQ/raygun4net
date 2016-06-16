using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.AspNetCore.Builders
{
  public class RaygunAspNetCoreResponseMessageBuilder
  {
    public static RaygunResponseMessage Build(HttpContext context)
    {
      var httpResponseFeature = context.Features.Get<IHttpResponseFeature>();
      return new RaygunResponseMessage
      {
        StatusCode = context.Response.StatusCode,
        StatusDescription = httpResponseFeature?.ReasonPhrase
      };
    }
  }
}