namespace Mindscape.Raygun4Net
{
  /// <summary>The W3C trace and span identifiers associated with an exception report.</summary>
  public sealed class RaygunTraceContext
  {
    /// <summary>The trace identifier, represented by 32 lowercase hexadecimal characters.</summary>
    public string TraceId { get; set; }

    /// <summary>The span identifier, represented by 16 lowercase hexadecimal characters.</summary>
    public string SpanId { get; set; }
  }
}
