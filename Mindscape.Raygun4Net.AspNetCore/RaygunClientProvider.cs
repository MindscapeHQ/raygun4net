namespace Mindscape.Raygun4Net
{
  public interface IRaygunAspNetCoreClientProvider
  {
    RaygunClient GetClient(RaygunSettings settings);
    RaygunSettings GetRaygunSettings(RaygunSettings baseSettings);
  }

  public class DefaultRaygunAspNetCoreClientProvider : IRaygunAspNetCoreClientProvider
  {
    public virtual RaygunClient GetClient(RaygunSettings settings)
    {
      return new RaygunClient(settings);
    }

    public virtual RaygunSettings GetRaygunSettings(RaygunSettings baseSettings)
    {
      return baseSettings;
    }
  }
}