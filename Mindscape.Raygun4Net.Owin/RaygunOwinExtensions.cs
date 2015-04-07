using System;
using Mindscape.Raygun4Net.Owin.Middleware;
using Owin;

namespace Mindscape.Raygun4Net.Owin
{
  public static class RaygunOwinExtensions
  {
    public static IAppBuilder UseRaygun(this IAppBuilder builder, Action<RaygunOwinClient> configuration)
    {
      if (builder == null)
      {
        throw new ArgumentNullException("builder");
      }

      if (configuration == null)
      {
        throw new ArgumentNullException("configuration");
      }

      var client = new RaygunOwinClient();
      configuration(client);

      return UseRaygun(builder, client);
    }

    public static IAppBuilder UseRaygun(this IAppBuilder builder)
    {
      return UseRaygun(builder, client => { });
    }

    public static IAppBuilder UseRaygun(this IAppBuilder builder, RaygunOwinClient client)
    {
      if (builder == null)
      {
        throw new ArgumentNullException("builder");
      }

      builder.Use(typeof(RaygunOwinExceptionMiddleware), client);
      //builder.Use(typeof(RaygunOwinErrorCodeMiddleware), client);

      return builder;
    }
  }
}