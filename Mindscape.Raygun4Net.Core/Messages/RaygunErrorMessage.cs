using System.Collections;
using Mindscape.Raygun4Net.Diagnostics;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorMessage
  {
    public RaygunErrorMessage InnerError { get; set; }

    public RaygunErrorMessage[] InnerErrors { get; set; }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunErrorStackTraceLineMessage[] StackTrace { get; set; }
    
    public PEDebugInformation[] Images { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunErrorMessage: InnerError={0}, InnerErrors={1}, Data={2}, ClassName={3}, Message={4}, StackTrace={5}, Images={6}]", InnerError, InnerErrors, Data, ClassName, Message, StackTrace, Images);
    }
  }
}
