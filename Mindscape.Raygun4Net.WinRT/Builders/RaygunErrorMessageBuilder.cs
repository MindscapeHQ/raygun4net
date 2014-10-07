using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder
  {
    public RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      message.Message = string.Format("{0}: {1}", exceptionType.Name, exception.Message);
      message.ClassName = exceptionType.FullName;

      message.StackTrace = BuildStackTrace(exception);
      message.Data = exception.Data;

      if (exception.InnerException != null)
      {
        message.InnerError = new RaygunErrorMessageBuilder().Build(exception.InnerException);
      }

      return message;
    }

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      string[] delim = { "\r\n" };
      string stackTrace = exception.StackTrace ?? exception.Data["Message"] as string;
      exception.Data.Remove("Message");
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
  }
}
