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

    public RaygunWebApiClientProvider(Func<RaygunWebApiClient> generateRaygunClient = null)
    {
      _generateRaygunClient = generateRaygunClient;
    }

    public RaygunWebApiClient GenerateRaygunWebApiClient()
    {
      return _generateRaygunClient == null ? new RaygunWebApiClient() : _generateRaygunClient();
    }
  }
}