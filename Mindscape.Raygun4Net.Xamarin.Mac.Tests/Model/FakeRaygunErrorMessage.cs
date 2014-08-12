using System;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Xamarin.Mac.Tests
{
  public class FakeRaygunErrorMessage : RaygunErrorMessage
  {
    public RaygunErrorStackTraceLineMessage[] ExposeParseStackTrace(string stackTrace)
    {
      return ParseStackTrace(stackTrace);
    }
  }
}

