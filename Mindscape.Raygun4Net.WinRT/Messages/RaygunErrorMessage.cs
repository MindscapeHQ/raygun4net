using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorMessage
  {
    public RaygunErrorMessage InnerError { get; set; }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunErrorStackTraceLineMessage[] StackTrace { get; set; }

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

    private RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
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

          int index = stackTraceLine.LastIndexOf(":line ");
          if (index > 0)
          {
            string number = stackTraceLine.Substring(index + 6);
            Int32.TryParse(number, out lineNumber);
            stackTraceLine = stackTraceLine.Substring(0, index);
          }
          index = stackTraceLine.LastIndexOf(") in ");
          if (index > 0)
          {
            fileName = stackTraceLine.Substring(index + 5);
            stackTraceLine = stackTraceLine.Substring(0, index);
          }
          index = stackTraceLine.IndexOf("(");
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
          stackTraceLineMessage.FileName = fileName;
          stackTraceLineMessage.LineNumber = lineNumber;
          lines.Add(stackTraceLineMessage);
        }
      }

      return lines.ToArray();
    }

    private string GenerateMethodName(MethodBase method)
    {
      var stringBuilder = new StringBuilder();

      stringBuilder.Append(method.Name);

      if (method is MethodInfo && method.IsGenericMethod)
      {
        Type[] genericArguments = method.GetGenericArguments();
        stringBuilder.Append("[");
        int index2 = 0;
        bool flag2 = true;
        for (; index2 < genericArguments.Length; ++index2)
        {
          if (!flag2)
            stringBuilder.Append(",");
          else
            flag2 = false;
          stringBuilder.Append(genericArguments[index2].Name);
        }
        stringBuilder.Append("]");
      }
      stringBuilder.Append("(");
      ParameterInfo[] parameters = method.GetParameters();
      bool flag3 = true;
      for (int index2 = 0; index2 < parameters.Length; ++index2)
      {
        if (!flag3)
          stringBuilder.Append(", ");
        else
          flag3 = false;
        string str2 = "<UnknownType>";
        if (parameters[index2].ParameterType != null)
          str2 = parameters[index2].ParameterType.Name;
        stringBuilder.Append(str2 + " " + parameters[index2].Name);
      }
      stringBuilder.Append(")");

      return stringBuilder.ToString();
    }
  }
}
