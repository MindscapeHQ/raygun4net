using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiDelegatingHandler : DelegatingHandler
  {
    public const string RequestBodyKey = "Raygun.RequestBody";

    protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
      var stream = await request.Content.ReadAsStreamAsync();
      if (stream != null && stream.CanSeek)
      {
        var lengthToRead = (int)(stream.Length < 4096 ? stream.Length : 4096);
        var buffer = new byte[lengthToRead];

        await stream.ReadAsync(buffer, 0, lengthToRead, cancellationToken);

        var body = Encoding.UTF8.GetString(buffer);
        if (!string.IsNullOrEmpty(body))
        {
          request.Properties[RequestBodyKey] = body;
        }

        stream.Seek(0, System.IO.SeekOrigin.Begin);
      }

      return await base.SendAsync(request, cancellationToken);
    }
  }
}