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
