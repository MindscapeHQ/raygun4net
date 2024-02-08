using System;
using System.Web;

namespace Mindscape.Raygun4Net.Tests
{
  public class FakeRaygunHttpModule : RaygunHttpModule
  {
    public bool ExposeCanSend(Exception exception)
    {
      return CanSend(exception);
    }

    public RaygunClient ExposeGetRaygunClient(HttpApplication application)
    {
      return GetRaygunClient(application);
    }
  }
}
