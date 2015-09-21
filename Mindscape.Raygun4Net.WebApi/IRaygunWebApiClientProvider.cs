using System;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  internal interface IRaygunWebApiClientProvider
  {
    RaygunWebApiClient GenerateRaygunWebApiClient(RaygunWebApiContext currentContext = null);
  }

  internal class RaygunWebApiClientProvider : IRaygunWebApiClientProvider
  {
    private readonly Func<RaygunWebApiContext, RaygunWebApiClient> _generateRaygunClient;
    private readonly string _applicationVersionFromAttach;

    public RaygunWebApiClientProvider(Func<RaygunWebApiContext, RaygunWebApiClient> generateRaygunClientWithHttpRequest,
      string applicationVersionFromAttach)
    {
      _generateRaygunClient = generateRaygunClientWithHttpRequest;

      _applicationVersionFromAttach = applicationVersionFromAttach;
    }

    public RaygunWebApiClient GenerateRaygunWebApiClient(RaygunWebApiContext currentContext = null)
    {
      RaygunWebApiClient client = null;

      if (_generateRaygunClient == null)
      {
        client = new RaygunWebApiClient();
      }
      else
      {
        client = _generateRaygunClient(currentContext);
      }

      if (client != null)
      {
        if (client.ApplicationVersion == null)
        {
          client.ApplicationVersion = _applicationVersionFromAttach;
        }
        client.CurrentHttpRequest(currentContext != null ? currentContext.RequestMessage : null);
      }

      return client;
    }
  }
}