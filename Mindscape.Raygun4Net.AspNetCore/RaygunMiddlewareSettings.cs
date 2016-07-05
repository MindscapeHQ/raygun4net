namespace Mindscape.Raygun4Net
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