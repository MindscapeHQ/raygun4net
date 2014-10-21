using System;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Xamarin.Mac.Tests
{
  public class FakeRaygunErrorMessage : RaygunErrorMessageBuilder
  {
    public RaygunErrorStackTraceLineMessage[] ExposeParseStackTrace(string stackTrace)
    {
      return ParseStackTrace(stackTrace);
    }
  }
}

