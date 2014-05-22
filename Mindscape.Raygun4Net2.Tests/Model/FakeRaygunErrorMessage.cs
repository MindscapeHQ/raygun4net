using System;
using System.Collections.Generic;
using System.Text;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net2.Tests
{
  public class FakeRaygunErrorMessage : RaygunErrorMessage
  {
    public RaygunErrorStackTraceLineMessage[] GetStackTrace(string stackTrace)
    {
      return ParseStackTrace(stackTrace);
    }
  }
}
