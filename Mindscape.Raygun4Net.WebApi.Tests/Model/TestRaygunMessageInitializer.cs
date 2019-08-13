using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.WebApi.Tests.Model
{
  public class TestRaygunMessageInitializer : IRaygunMessageInitializer
  {
    public void Initialize(RaygunMessage raygunMessage)
    {
      if (raygunMessage.Details.UserCustomData == null)
      {
        raygunMessage.Details.UserCustomData = new Dictionary<string, object>();
      }
      raygunMessage.Details.UserCustomData["initializerTestData"] = "true";
    }
  }
}