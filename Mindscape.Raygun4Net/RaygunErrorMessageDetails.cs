using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunErrorMessageDetails
  {
    public RaygunErrorMessageDetails(Exception exception)
    {
      Message = exception.Message;
      StackTrace = exception.StackTrace;
    }

    public string Message { get; set; }

    public string StackTrace { get; set; }
  }
}