using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net;

namespace Mindscape.Raygun4Net2.Tests
{
  public class FakeHttpApplication : HttpApplication, IRaygunApplication
  {
    public RaygunClient GenerateRaygunClient()
    {
      return new RaygunClient() { User = "TestUser" };
    }
  }
}
