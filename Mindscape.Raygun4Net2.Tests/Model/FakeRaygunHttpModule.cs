using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net;

namespace Mindscape.Raygun4Net2.Tests
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
