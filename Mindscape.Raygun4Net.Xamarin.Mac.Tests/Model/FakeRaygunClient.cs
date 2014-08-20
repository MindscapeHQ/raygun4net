using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Xamarin.Mac.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient()
      :this("tempkey")
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

    public bool ExposeOnSendingMessage(RaygunMessage raygunMessage)
    {
      return OnSendingMessage(raygunMessage);
    }
  }
}

