using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient() { }

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
