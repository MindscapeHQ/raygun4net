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
      RaygunWebApiClient client = null;

      if (_generateRaygunClient == null)
      {
        client = new RaygunWebApiClient();
      }
      else
      {
        client = _generateRaygunClient(currentRequest);
      }

      if (client != null)
      {
        if (client.ApplicationVersion == null)
        {
          client.ApplicationVersion = _applicationVersionFromAttach;
        }
        client.SetCurrentHttpRequest(currentRequest);
      }

      return client;
    }
  }
}