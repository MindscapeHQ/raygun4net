using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.WebApi.Tests.Model
{
  public class FakeRaygunWebApiClient : RaygunWebApiClient
  {
    public bool ExposeCanSend(RaygunMessage message)
    {
      return CanSend(message);
    }

    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return base.StripWrapperExceptions(exception);
    }
  }
}
