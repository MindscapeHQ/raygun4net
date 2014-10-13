using System;

namespace Mindscape.Raygun4Net.WebApi
{
  internal interface IRaygunWebApiClientProvider
  {
    RaygunWebApiClient GenerateRaygunWebApiClient();
  }

  internal class RaygunWebApiClientProvider : IRaygunWebApiClientProvider
  {
    private readonly Func<RaygunWebApiClient> _generateRaygunClient;
    private readonly string _applicationVersionFromAttach;

    public RaygunWebApiClientProvider(Func<RaygunWebApiClient> generateRaygunClient = null, string applicationVersionFromAttach = null)
    {
      _generateRaygunClient = generateRaygunClient;
      _applicationVersionFromAttach = applicationVersionFromAttach;
    }

    public RaygunWebApiClient GenerateRaygunWebApiClient()
    {
      if (_generateRaygunClient == null)
      {
        return new RaygunWebApiClient { ApplicationVersion = _applicationVersionFromAttach };
      }

      var client = _generateRaygunClient();
      if(client.ApplicationVersion == null)
      {
        client.ApplicationVersion = _applicationVersionFromAttach;
      }
      return client;
    }
  }
}