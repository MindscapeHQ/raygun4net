using System;
using Mindscape.Raygun4Net.Messages;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;

namespace Mindscape.Raygun4Net.Xamarin.Mac.Unified.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient()
      : this("tempkey")
    {
    }

    public FakeRaygunClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunMessage ExposeBuildMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData);
    }

    public bool ExposeOnSendingMessage(RaygunMessage message)
    {
      return OnSendingMessage(message);
    }

    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return StripWrapperExceptions(exception);
    }
  }
}

