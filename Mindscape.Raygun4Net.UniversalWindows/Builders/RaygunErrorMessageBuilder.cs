using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder
  {
    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      message.Message = exception.Message;
      message.ClassName = FormatTypeName(exceptionType, true);

      message.StackTrace = BuildStackTrace(exception);
      message.Data = exception.Data;

      AggregateException ae = exception as AggregateException;
      if (ae != null && ae.InnerExceptions != null)
      {
        message.InnerErrors = new RaygunErrorMessage[ae.InnerExceptions.Count];
        int index = 0;
        foreach (Exception e in ae.InnerExceptions)
        {
          message.InnerErrors[index] = Build(e);
          index++;
        }
      }
      else if (exception.InnerException != null)
      {
        message.InnerError = Build(exception.InnerException);
      }

      return message;
    }

    private static string FormatTypeName(Type type, bool fullName)
    {
      string name = fullName ? type.FullName : type.Name;
      Type[] genericArguments = type.GenericTypeArguments;
      if (genericArguments.Length == 0)
      {
        return name;
      }

      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(name.Substring(0, name.IndexOf("`")));
      stringBuilder.Append("<");
      foreach (Type t in genericArguments)
      {
        stringBuilder.Append(FormatTypeName(t, false)).Append(",");
      }
      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      stringBuilder.Append(">");

      return stringBuilder.ToString();
    }

    /*private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
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
    }*/

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      var stackTrace = new StackTrace(exception, true);
      var frames = stackTrace.GetFrames();

      if (frames == null || frames.Length == 0)
      {
        var line = new RaygunErrorStackTraceLineMessage { FileName = "none", LineNumber = 0 };
        lines.Add(line);

        return lines.ToArray();
      }

      foreach (StackFrame frame in frames)
      {
        MethodBase method = frame.GetMethod();

        if (method != null)
        {
          int lineNumber = frame.GetFileLineNumber();

          if (lineNumber == 0)
          {
            lineNumber = frame.GetILOffset();
          }

          var methodName = GenerateMethodName(method);

          string file = frame.GetFileName();

          string className = method.DeclaringType != null ? method.DeclaringType.FullName : "(unknown)";

          var line = new RaygunErrorStackTraceLineMessage
          {
            FileName = file,
            LineNumber = lineNumber,
            MethodName = methodName,
            ClassName = className
          };

          lines.Add(line);
        }
      }

      return lines.ToArray();
    }

    protected static string GenerateMethodName(MethodBase method)
    {
      var stringBuilder = new StringBuilder();

      stringBuilder.Append(method.Name);

      bool first = true;

      if (method is MethodInfo && method.IsGenericMethod)
      {
        Type[] genericArguments = method.GetGenericArguments();
        stringBuilder.Append("[");

        for (int i = 0; i < genericArguments.Length; i++)
        {
          if (!first)
          {
            stringBuilder.Append(",");
          }
          else
          {
            first = false;
          }

          stringBuilder.Append(genericArguments[i].Name);
        }

        stringBuilder.Append("]");
      }

      stringBuilder.Append("(");

      ParameterInfo[] parameters = method.GetParameters();

      first = true;

      for (int i = 0; i < parameters.Length; ++i)
      {
        if (!first)
        {
          stringBuilder.Append(", ");
        }
        else
        {
          first = false;
        }

        string type = "<UnknownType>";

        if (parameters[i].ParameterType != null)
        {
          type = parameters[i].ParameterType.Name;
        }

        stringBuilder.Append(type + " " + parameters[i].Name);
      }

      stringBuilder.Append(")");

      return stringBuilder.ToString();
    }
  }
}
