namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunMiddlewareSettings
  {
    public IRaygunAspNetCoreClientProvider ClientProvider { get; set; }

    public RaygunMiddlewareSettings()
    {
      ClientProvider = new DefaultRaygunAspNetCoreClientProvider();
    }
  }
}