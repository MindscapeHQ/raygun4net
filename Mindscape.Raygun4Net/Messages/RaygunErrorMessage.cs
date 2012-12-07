using System;
using System.Collections;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorMessage
  {
    public RaygunErrorMessage(Exception exception)
    {
      var exceptionType = exception.GetType();

      Message = string.Format("{0}: {1}", exceptionType.Name, exception.Message);
      StackTrace = exception.StackTrace;
      ClassName = exceptionType.FullName;
      Data = exception.Data;

      // Missing: filename, catchingMethod, stack trace needs to be split up
    }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public string StackTrace { get; set; }
  }
}