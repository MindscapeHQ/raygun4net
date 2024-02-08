using System;
using System.Web;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  public class FakeRaygunHttpModule : RaygunHttpModule
  {
    public RaygunClient ExposeGetRaygunClient(HttpApplication application)
    {
      return GetRaygunClient(application);
    }

    public bool ExposeCanSend(Exception exception)
    {
      return CanSend(exception);
    }
  }
}
