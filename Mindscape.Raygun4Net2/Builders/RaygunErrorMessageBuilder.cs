using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder : RaygunErrorMessageBuilderBase
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
  }
}
