using System;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  internal class RaygunWebApiClientProvider : IRaygunWebApiClientProvider
  {
    private readonly Func<RaygunWebApiClient> _generateRaygunClient;
    private readonly string _applicationVersionFromAttach;

    public RaygunWebApiClientProvider(Func<RaygunWebApiClient> generateRaygunClientWithHttpRequest, string applicationVersionFromAttach)
    {
      _generateRaygunClient = generateRaygunClientWithHttpRequest;
      _applicationVersionFromAttach = applicationVersionFromAttach;
    }

    public RaygunWebApiClient GenerateRaygunWebApiClient()
    {
      RaygunWebApiClient client = null;

      if (_generateRaygunClient == null)
      {
        client = new RaygunWebApiClient();
        client.ApplicationVersion = _applicationVersionFromAttach;
      }
      else
      {
        client = _generateRaygunClient();
      }
      
      return client;
    }
  }
}