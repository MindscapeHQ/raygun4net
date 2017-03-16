using System.Linq;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiDelegatingHandler : DelegatingHandler
  {
    public const string RequestBodyKey = "Raygun.RequestBody";

    protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
      if (IsText(request))
      {
        var body = await request.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(body))
        {
          request.Properties[RequestBodyKey] = body.Length > 4096 ? body.Substring(0, 4096) : body;
        }
      }

      return await base.SendAsync(request, cancellationToken);
    }

    private bool IsText(HttpRequestMessage request)
    {
      var mediaType = request.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
      if (mediaType == null)
        return false;
      if (mediaType == "text/xml" || mediaType == "application/xml" || mediaType == "application/json")
        return true;

      return mediaType.StartsWith("text/");
    }
  }
}