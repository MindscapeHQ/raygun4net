using System;
using Mindscape.Raygun4Net.Messages;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder : RaygunErrorMessageBuilderBase
  {
    public RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage ();

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

      string stackTraceStr = exception.StackTrace;
      if (stackTraceStr == null)
      {
        var line = new RaygunErrorStackTraceLineMessage { FileName = "none", LineNumber = 0 };
        lines.Add(line);
        return lines.ToArray();
      }

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

      string[] stackTraceLines = stackTrace.Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);
      foreach (string stackTraceLine in stackTraceLines)
      {
        int lineNumber = 0;
        string fileName = null;
        string methodName = null;
        string className = null;
        string stackTraceLn = stackTraceLine;
        // Line number
        int index = stackTraceLine.LastIndexOf(":");
        if (index > 0)
        {
          bool success = int.TryParse(stackTraceLn.Substring(index + 1), out lineNumber);
          if (success)
          {
            stackTraceLn = stackTraceLn.Substring(0, index);
          }
        }
        // File name
        index = stackTraceLn.LastIndexOf("] in ");
        if (index > 0)
        {
          fileName = stackTraceLn.Substring(index + 5);
          stackTraceLn = stackTraceLn.Substring(0, index);
        }
        // Method name
        index = stackTraceLn.LastIndexOf("(");
        if (index > 0 && !stackTraceLine.StartsWith("at ("))
        {
          index = stackTraceLn.LastIndexOf(".", index);
          if (index > 0)
          {
            int endIndex = stackTraceLn.IndexOf("[0x");
            if (endIndex < 0)
            {
              endIndex = stackTraceLn.Length;
            }
            methodName = stackTraceLn.Substring(index + 1, endIndex - index - 1).Trim();
            methodName = methodName.Replace(" (", "(");
            stackTraceLn = stackTraceLn.Substring(0, index);

            // Memory address
            index = methodName.LastIndexOf("<0x");
            if (index >= 0)
            {
              fileName = methodName.Substring(index);
              methodName = methodName.Substring(0, index).Trim();
            }

            // Class name
            index = stackTraceLn.IndexOf("at ");
            if (index >= 0)
            {
              className = stackTraceLn.Substring(index + 3);
            }
          }
        }

        if (methodName == null && fileName == null)
        {
          if (!String.IsNullOrWhiteSpace(stackTraceLn) && stackTraceLn.StartsWith("at "))
          {
            stackTraceLn = stackTraceLn.Substring(3);
          }
          fileName = stackTraceLn;
        }

        if ("<filename unknown>".Equals(fileName))
        {
          fileName = null;
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

      return lines.ToArray();
    }
  }
}

