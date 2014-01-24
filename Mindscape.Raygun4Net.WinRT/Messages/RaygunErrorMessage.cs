using System;
using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorMessage
  {
    public RaygunErrorMessage()
    {
    }

    public RaygunErrorMessage(Exception exception)
    {
      var exceptionType = exception.GetType();

      Message = string.Format("{0}: {1}", exceptionType.Name, exception.Message);
      ClassName = exceptionType.FullName;

      StackTrace = BuildStackTrace(exception);
      Data = exception.Data;

      if (exception.InnerException != null)
      {
        InnerError = new RaygunErrorMessage(exception.InnerException);
      }
    }

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      string[] delim = { "\r\n" };
      string stackTrace = exception.StackTrace ?? exception.Data["Message"] as string;
      if (stackTrace != null)
      {
        var frames = stackTrace.Split(delim, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in frames)
        {
          // Trim the stack trace line
          string stackTraceLine = line.Trim();
          if (stackTraceLine.StartsWith("at "))
          {
            stackTraceLine = stackTraceLine.Substring(3);
          }

          int lineNumber = 0;
          string className = stackTraceLine;
          string methodName = null;
          string fileName = null;

          int index = stackTraceLine.LastIndexOf(":line ", StringComparison.Ordinal);
          if (index > 0)
          {
            string number = stackTraceLine.Substring(index + 6);
            Int32.TryParse(number, out lineNumber);
            stackTraceLine = stackTraceLine.Substring(0, index);
          }
          index = stackTraceLine.LastIndexOf(") in ", StringComparison.Ordinal);
          if (index > 0)
          {
            fileName = stackTraceLine.Substring(index + 5);
            stackTraceLine = stackTraceLine.Substring(0, index + 1);
          }
          index = stackTraceLine.IndexOf("(", StringComparison.Ordinal);
          if (index > 0)
          {
            index = stackTraceLine.LastIndexOf(".", index, StringComparison.Ordinal);
            if (index > 0)
            {
              className = stackTraceLine.Substring(0, index);
              methodName = stackTraceLine.Substring(index + 1);
            }
          }

          RaygunErrorStackTraceLineMessage stackTraceLineMessage = new RaygunErrorStackTraceLineMessage();
          stackTraceLineMessage.ClassName = className;
          stackTraceLineMessage.MethodName = methodName;
          stackTraceLineMessage.FileName = fileName;
          stackTraceLineMessage.LineNumber = lineNumber;
          lines.Add(stackTraceLineMessage);
        }
      }

      return lines.ToArray();
    }

    public RaygunErrorMessage InnerError { get; set; }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunErrorStackTraceLineMessage[] StackTrace { get; set; }
  }
}
