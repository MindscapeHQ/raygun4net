using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiDelegatingHandler : DelegatingHandler
  {
    protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
      var body = await request.Content.ReadAsStringAsync();
      if (!string.IsNullOrEmpty(body))
      {
        request.Properties["body"] = body;
      }

      return await base.SendAsync(request, cancellationToken);
    }
  }
}