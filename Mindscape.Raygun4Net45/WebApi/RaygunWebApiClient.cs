using System.Net.Http;
using System.Threading;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiClient : RaygunClientBase
  {
    private readonly ThreadLocal<HttpRequestMessage> _currentWebRequest = new ThreadLocal<HttpRequestMessage>(() => null);

    public RaygunWebApiClient(string apiKey) : base(apiKey) { }
    public RaygunWebApiClient() { }

    public RaygunClientBase CurrentHttpRequest(HttpRequestMessage request)
    {
      _currentWebRequest.Value = request;
      return this;
    }

    protected override IRaygunMessageBuilder BuildMessageCore()
    {
      return RaygunWebApiMessageBuilder.New
        .SetHttpDetails(_currentWebRequest.Value, _requestMessageOptions);
    }
  }
}