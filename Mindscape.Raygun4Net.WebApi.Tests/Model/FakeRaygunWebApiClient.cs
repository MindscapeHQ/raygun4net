using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections;
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

    public RaygunMessage ExposeBuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime? currentTime = null)
    {
      return base.BuildMessage(exception, tags, userCustomData, currentTime);
    }
  }
}
