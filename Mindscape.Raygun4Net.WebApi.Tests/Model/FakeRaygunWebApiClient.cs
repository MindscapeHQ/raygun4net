using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.WebApi.Tests.Model
{
  public class FakeRaygunWebApiClient : RaygunWebApiClient
  {
    public bool ExposeCanSend(RaygunMessage message)
    {
      return CanSend(message);
    }
  }
}
