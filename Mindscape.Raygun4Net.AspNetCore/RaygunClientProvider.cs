namespace Mindscape.Raygun4Net.AspNetCore
{
  public interface IRaygunAspNetCoreClientProvider
  {
    RaygunAspNetCoreClient GetClient(RaygunSettings settings);
    RaygunSettings GetRaygunSettings(RaygunSettings baseSettings);
  }

  public class DefaultRaygunAspNetCoreClientProvider : IRaygunAspNetCoreClientProvider
  {
    public virtual RaygunAspNetCoreClient GetClient(RaygunSettings settings)
    {
      return new RaygunAspNetCoreClient(settings);
    }

    public virtual RaygunSettings GetRaygunSettings(RaygunSettings baseSettings)
    {
      return baseSettings;
    }
  }
}