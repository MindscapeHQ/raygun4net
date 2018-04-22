using System.Collections;

namespace Mindscape.Raygun4Net.NetCore.Messages
{
  public class RaygunErrorMessage
  {
    public RaygunErrorMessage InnerError { get; set; }

    public RaygunErrorMessage[] InnerErrors { get; set; }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunErrorStackTraceLineMessage[] StackTrace { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return $"[RaygunErrorMessage: InnerError={InnerError}, InnerErrors={InnerErrors}, Data={Data}, ClassName={ClassName}, Message={Message}, StackTrace={StackTrace}]";
    }
  }
}
