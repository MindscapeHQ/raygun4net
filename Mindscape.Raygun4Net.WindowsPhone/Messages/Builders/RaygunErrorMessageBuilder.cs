using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunErrorMessageBuilder
  {
    public RaygunErrorMessage Build(Exception exception)
    {
      var raygunErrorMessage = new RaygunErrorMessage();
      var exceptionType = exception.GetType();

      raygunErrorMessage.Message = exception.Message;
      raygunErrorMessage.ClassName = exceptionType.FullName;

      raygunErrorMessage.StackTrace = BuildStackTrace(exception);
      raygunErrorMessage.Data = exception.Data;

      if (exception.InnerException != null)
      {
        raygunErrorMessage.InnerError = new RaygunErrorMessageBuilder().Build(exception.InnerException);
      }

      return raygunErrorMessage;
    }

    private RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      if (exception.StackTrace != null)
      {
        char[] delim = { '\r', '\n' };
        var frames = exception.StackTrace.Split(delim, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in frames)
        {
          // Trim the stack trace line
          string stackTraceLine = line.Trim();
          if (stackTraceLine.StartsWith("at "))
          {
            stackTraceLine = stackTraceLine.Substring(3);
          }

          string className = stackTraceLine;
          string methodName = null;

          // Extract the method name and class name if possible:
          int index = stackTraceLine.IndexOf("(");
          if (index > 0)
          {
            index = stackTraceLine.LastIndexOf(".", index);
            if (index > 0)
            {
              className = stackTraceLine.Substring(0, index);
              methodName = stackTraceLine.Substring(index + 1);
            }
          }

          RaygunErrorStackTraceLineMessage stackTraceLineMessage = new RaygunErrorStackTraceLineMessage();
          stackTraceLineMessage.ClassName = className;
          stackTraceLineMessage.MethodName = methodName;
          lines.Add(stackTraceLineMessage);
        }
      }

      return lines.ToArray();
    }
  }
}
