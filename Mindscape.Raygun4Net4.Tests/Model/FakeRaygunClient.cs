using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net;

namespace Mindscape.Raygun4Net4.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient()
    {
    }

    public FakeRaygunClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunRequestMessageOptions ExposeRequestMessageOptions
    {
      get { return _requestMessageOptions; }
    }

    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return base.StripWrapperExceptions(exception);
    }
  }
}
