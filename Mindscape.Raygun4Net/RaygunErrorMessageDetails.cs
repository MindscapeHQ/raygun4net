using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunErrorMessageDetails
  {
    public RaygunErrorMessageDetails(Exception exception)
    {
      var exceptionType = exception.GetType();

      Message = string.Format("{0}: {1}", exceptionType.Name, exception.Message);
      StackTrace = exception.StackTrace;
      ClassName = exceptionType.FullName;
    }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public string StackTrace { get; set; }
  }
}