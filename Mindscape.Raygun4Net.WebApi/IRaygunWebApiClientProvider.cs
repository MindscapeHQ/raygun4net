using System;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  internal interface IRaygunWebApiClientProvider
  {
    RaygunWebApiClient GenerateRaygunWebApiClient(HttpRequestMessage currentRequest = null);
  }

  internal class RaygunWebApiClientProvider : IRaygunWebApiClientProvider
  {
    private readonly Func<HttpRequestMessage, RaygunWebApiClient> _generateRaygunClient;
    private readonly string _applicationVersionFromAttach;

    public RaygunWebApiClientProvider(Func<HttpRequestMessage, RaygunWebApiClient> generateRaygunClientWithHttpRequest,
      string applicationVersionFromAttach)
    {
      _generateRaygunClient = generateRaygunClientWithHttpRequest;

      _applicationVersionFromAttach = applicationVersionFromAttach;
    }

    public RaygunWebApiClient GenerateRaygunWebApiClient(HttpRequestMessage currentRequest = null)
    {
      if (_generateRaygunClient == null)
      {
        return new RaygunWebApiClient { ApplicationVersion = _applicationVersionFromAttach };
      }

      var client = _generateRaygunClient(currentRequest);
      if(client.ApplicationVersion == null)
      {
        client.ApplicationVersion = _applicationVersionFromAttach;
      }
      client.CurrentHttpRequest(currentRequest);

      return client;
    }
  }
}