using System.Collections;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorMessage
  {
    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunErrorStackTraceLineMessage[] StackTrace { get; set; }
  }
}