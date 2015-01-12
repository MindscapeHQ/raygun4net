using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder : RaygunErrorMessageBuilderBase
  {
    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      message.Message = exception.Message;
      message.ClassName = exceptionType.FullName;

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

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
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
        string[] stackTraceLines = stackTraceStr.Split('\n');
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
              // File name
              index = stackTraceLn.LastIndexOf("] in ");
              if (index > 0)
              {
                fileName = stackTraceLn.Substring(index + 5);
                if ("<filename unknown>".Equals(fileName))
                {
                  fileName = null;
                }
                stackTraceLn = stackTraceLn.Substring(0, index);
                // Method name
                index = stackTraceLn.LastIndexOf("(");
                if (index > 0)
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
                  }
                }
                // Class name
                index = stackTraceLn.IndexOf("at ");
                if (index >= 0)
                {
                  className = stackTraceLn.Substring(index + 3);
                }
              }
              else
              {
                fileName = stackTraceLn;
              }
            }
            else
            {
              index = stackTraceLn.IndexOf("at ");
              if (index >= 0)
              {
                index += 3;
              }
              else
              {
                index = 0;
              }
              fileName = stackTraceLn.Substring(index);
            }
          }
          else
          {
            fileName = stackTraceLn;
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
        if (lines.Count > 0)
        {
          return lines.ToArray();
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

          string className = method.ReflectedType != null
                       ? method.ReflectedType.FullName
                       : "(unknown)";

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
  }
}