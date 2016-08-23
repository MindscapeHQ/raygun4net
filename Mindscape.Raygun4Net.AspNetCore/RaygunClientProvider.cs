using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunAspNetCoreClientProvider
  {
    RaygunClient GetClient(RaygunSettings settings, HttpContext context = null);
    RaygunSettings GetRaygunSettings(RaygunSettings baseSettings);
  }

  public class DefaultRaygunAspNetCoreClientProvider : IRaygunAspNetCoreClientProvider
  {
    public virtual RaygunClient GetClient(RaygunSettings settings, HttpContext context = null)
    {
      return new RaygunClient(settings, context);
    }

    public virtual RaygunSettings GetRaygunSettings(RaygunSettings baseSettings)
    {
      return baseSettings;
    }
  }
}