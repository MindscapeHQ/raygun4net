using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunAspNetCoreClientProvider
  {
    AspNetCore.RaygunClient GetClient(AspNetCore.RaygunSettings settings);
    Mindscape.Raygun4Net.AspNetCore.RaygunClient GetClient(AspNetCore.RaygunSettings settings, HttpContext context);
    AspNetCore.RaygunSettings GetRaygunSettings(AspNetCore.RaygunSettings baseSettings);
  }

  public class DefaultRaygunAspNetCoreClientProvider : IRaygunAspNetCoreClientProvider
  {
    public virtual Mindscape.Raygun4Net.AspNetCore.RaygunClient GetClient(AspNetCore.RaygunSettings settings)
    {
      return GetClient(settings, null);
    }

    public virtual Mindscape.Raygun4Net.AspNetCore.RaygunClient GetClient(AspNetCore.RaygunSettings settings, HttpContext context)
    {
      return new Mindscape.Raygun4Net.AspNetCore.RaygunClient(settings, context);
    }

    public virtual AspNetCore.RaygunSettings GetRaygunSettings(AspNetCore.RaygunSettings baseSettings)
    {
      return baseSettings;
    }
  }
}