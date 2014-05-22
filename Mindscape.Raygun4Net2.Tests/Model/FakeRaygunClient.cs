using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net2.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunMessage CreateMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData);
    }

    public bool Validate()
    {
      return ValidateApiKey();
    }
  }
}
