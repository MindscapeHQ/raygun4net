using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mindscape.Raygun4Net4.Integration.Tests.Setup;

[ExcludeFromCodeCoverage]
internal class TraceChecker : TraceListener
{
  public List<string> Traces { get; } = new();

  public override void Write(string message)
  {
    Traces.Add(message);
  }

  public override void WriteLine(string message)
  {
    Traces.Add(message);
  }

  public void Clear()
  {
    Traces.Clear();
  }
}