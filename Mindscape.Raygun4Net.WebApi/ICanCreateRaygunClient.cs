using System;

namespace Mindscape.Raygun4Net.WebApi
{
  public interface ICanCreateRaygunClient
  {
    RaygunWebApiClient GetClient();
  }

  public class CanCreateRaygunClient : ICanCreateRaygunClient
  {
    private readonly Func<RaygunWebApiClient> _generateRaygunClient;

    public CanCreateRaygunClient(Func<RaygunWebApiClient> generateRaygunClient = null)
    {
      _generateRaygunClient = generateRaygunClient;
    }

    public RaygunWebApiClient GetClient()
    {
      return _generateRaygunClient == null ? new RaygunWebApiClient() : _generateRaygunClient();
    }
  }
}