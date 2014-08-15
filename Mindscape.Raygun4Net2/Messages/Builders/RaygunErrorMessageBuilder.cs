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

      try
      {
        RaygunErrorStackTraceLineMessage[] array = ParseStackTrace(exception.StackTrace);
        if (array.Length > 0)
        {
          return array;
        }
      }
      catch { }



      var stackTrace = new StackTrace(exception, true);
      var frames = stackTrace.GetFrames();

      if (frames == null || frames.Length == 0)
      {
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

          string className = method.ReflectedType != null ? method.ReflectedType.FullName : "(unknown)";

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

    protected RaygunErrorStackTraceLineMessage[] ParseStackTrace(string stackTrace)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      if (stackTrace == null)
      {
        return lines.ToArray();
      }

      string[] stackTraceLines = stackTrace.Split('\r', '\n');
      foreach (string stackTraceLine in stackTraceLines)
      {
        if (!String.IsNullOrEmpty(stackTraceLine))
        {
          int lineNumber = 0;
          string fileName = null;
          string methodName = null;
          string className = null;
          string stackTraceLn = stackTraceLine;
          // Line number
          int index = stackTraceLine.LastIndexOf(":line ");
          if (index > 0)
          {
            bool success = int.TryParse(stackTraceLn.Substring(index + 6), out lineNumber);
            stackTraceLn = stackTraceLn.Substring(0, index);
          }
          // File name
          index = stackTraceLn.LastIndexOf(") in ");
          if (index > 0)
          {
            fileName = stackTraceLn.Substring(index + 5);
            if ("<filename unknown>".Equals(fileName))
            {
              fileName = null;
            }
            stackTraceLn = stackTraceLn.Substring(0, index + 1);
          }
          // Method name
          index = stackTraceLn.LastIndexOf("(");
          if (index > 0)
          {
            index = stackTraceLn.LastIndexOf(".", index);
            if (index > 0)
            {
              int endIndex = stackTraceLn.LastIndexOf(")");
              if (endIndex > 0)
              {
                methodName = stackTraceLn.Substring(index + 1, endIndex - index).Trim();
                methodName = methodName.Replace(" (", "(");
                stackTraceLn = stackTraceLn.Substring(0, index);
              }
            }
          }
          // Class name
          if (methodName != null)
          {
            index = stackTraceLn.IndexOf("at ");
            if (index >= 0)
            {
              className = stackTraceLn.Substring(index + 3);
            }
          }
          else if (fileName == null)
          {
            fileName = stackTraceLine.Trim();
          }
          var line = new RaygunErrorStackTraceLineMessage
          {
            FileName = fileName,
            LineNumber = lineNumber,
            MethodName = methodName,
            ClassName = className
          };

          lines.Add(line);
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
