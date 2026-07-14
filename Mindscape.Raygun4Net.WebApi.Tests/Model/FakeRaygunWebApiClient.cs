using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.WebApi.Tests.Model
{
  public class FakeRaygunWebApiClient : RaygunWebApiClient
  {
    public FakeRaygunWebApiClient()
    {
    }

    public FakeRaygunWebApiClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunRequestMessageOptions ExposeRequestMessageOptions
    {
      get { return _requestMessageOptions; }
    }

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
