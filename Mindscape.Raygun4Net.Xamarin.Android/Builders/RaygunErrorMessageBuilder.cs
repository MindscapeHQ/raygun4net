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
using System.Runtime.CompilerServices;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder : RaygunErrorMessageBuilderBase
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
          RaygunErrorStackTraceLineMessage stackTraceLineMessage = ParseStackTraceLine(stackTraceLine);
          if (stackTraceLineMessage != null)
          {
            lines.Add(stackTraceLineMessage);
          }
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

    internal static RaygunErrorStackTraceLineMessage ParseStackTraceLine(string stackTraceLine)
    {
      int lineNumber = 0;
      string fileName = null;
      string methodName = null;
      string className = null;
      string stackTraceLn = stackTraceLine.Trim();
      bool bracketAfterLineNumber = false;
      if (!stackTraceLn.StartsWith("at (wrapper") && !stackTraceLn.StartsWith("(wrapper") && stackTraceLn.StartsWith("at "))
      {
        // Line number
        int index = stackTraceLn.LastIndexOf(":");
        if (index > 0)
        {
          bracketAfterLineNumber = stackTraceLn.EndsWith(")");
          int length = bracketAfterLineNumber ? stackTraceLn.Length - index - 2 : stackTraceLn.Length - index - 1;
          bool success = int.TryParse(stackTraceLn.Substring(index + 1, Math.Max(0, length)), out lineNumber);
          if (success)
          {
            stackTraceLn = stackTraceLn.Substring(0, index);
          }
          else
          {
            bracketAfterLineNumber = false;
          }
        }

        // File name
        int fileSeparatorLength = 0;
        if (bracketAfterLineNumber)
        {
          index = index = stackTraceLn.LastIndexOf("(");
          fileSeparatorLength = 1;
        }
        if (!bracketAfterLineNumber || index < 0)
        {
          index = stackTraceLn.LastIndexOf(" in ");
          fileSeparatorLength = 4;
        }
        if (index > 0)
        {
          fileName = stackTraceLn.Substring(index + fileSeparatorLength);
          if ("<filename unknown>".Equals(fileName))
          {
            fileName = null;
          }
          stackTraceLn = stackTraceLn.Substring(0, index);
        }

        // Method name
        string parameters = "";
        int parameterStartIndex = stackTraceLn.LastIndexOf("(");
        if (parameterStartIndex > 0)
        {
          int parameterEndIndex = stackTraceLn.IndexOf("[0x") - 1;
          if (parameterEndIndex < 0)
          {
            parameterEndIndex = stackTraceLn.IndexOf("<0x") - 1;
            if (parameterEndIndex < 0)
            {
              parameterEndIndex = stackTraceLn.Length - 1;
            }
          }
          parameters = stackTraceLn.Substring(parameterStartIndex, parameterEndIndex - parameterStartIndex + 1).Trim();
        }
        else
        {
          parameterStartIndex = stackTraceLn.Length;
        }

        int methodStartIndex = stackTraceLn.IndexOf('<') + 1;
        int methodEndIndex = methodStartIndex;
        int methodSeparatorLength = 0;
        if (methodStartIndex > 0 && methodStartIndex < parameterStartIndex)
        {
          methodEndIndex = stackTraceLn.IndexOf('>', methodStartIndex) - 1;
          methodSeparatorLength = 2;
        }

        if (methodEndIndex - methodStartIndex <= 0)
        {
          methodStartIndex = stackTraceLn.LastIndexOf(".", parameterStartIndex) + 1;
          methodEndIndex = parameterStartIndex - 1;
          methodSeparatorLength = 1;
        }

        if (methodStartIndex > 0)
        {
          methodName = stackTraceLn.Substring(methodStartIndex, methodEndIndex - methodStartIndex + 1).Trim();
          methodName += parameters;
          stackTraceLn = stackTraceLn.Substring(0, methodStartIndex - methodSeparatorLength);
        }

        // Class name
        index = stackTraceLn.IndexOf("at ");
        if (index >= 0)
        {
          className = stackTraceLn.Substring(index + 3);
        }
      }

      if (lineNumber != 0 || !String.IsNullOrWhiteSpace(methodName) || !String.IsNullOrWhiteSpace(fileName) || !String.IsNullOrWhiteSpace(className))
      {
        var line = new RaygunErrorStackTraceLineMessage
        {
          FileName = fileName,
          LineNumber = lineNumber,
          MethodName = methodName,
          ClassName = className,
        };

        return line;
      }
      else if (!String.IsNullOrWhiteSpace(stackTraceLn))
      {
        if (stackTraceLn.StartsWith("at "))
        {
          stackTraceLn = stackTraceLn.Substring(3);
        }
        var line = new RaygunErrorStackTraceLineMessage
        {
          FileName = stackTraceLn
        };

        return line;
      }

      return null;
    }
  }
}