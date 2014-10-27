using System;
using System.Collections.Generic;
using System.Text;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net2.Tests
{
  public class FakeRaygunErrorMessageBuilder : RaygunErrorMessageBuilder
  {
    public RaygunErrorStackTraceLineMessage[] ExposeParseStackTrace(string stackTrace)
    {
      return ParseStackTrace(stackTrace);
    }
  }
}
