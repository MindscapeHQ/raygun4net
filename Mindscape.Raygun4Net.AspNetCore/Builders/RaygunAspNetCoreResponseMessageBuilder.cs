using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.AspNet5.Builders
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