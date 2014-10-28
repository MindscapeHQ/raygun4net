using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.WebApi;

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
