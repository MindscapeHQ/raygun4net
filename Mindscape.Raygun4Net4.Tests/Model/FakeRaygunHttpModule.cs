using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net;

namespace Mindscape.Raygun4Net4.Tests
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
