using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Mindscape.Raygun4Net.Tests
{
  public class FakeHttpApplication : HttpApplication, IRaygunApplication
  {
    public RaygunClient GenerateRaygunClient()
    {
      return new RaygunClient() { User = "TestUser" };
    }
  }
}
