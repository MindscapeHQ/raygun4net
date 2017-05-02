using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Mindscape.Raygun4Net.Messages;
#if __UNIFIED__
using Foundation;
using ObjCRuntime;
#else
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
#endif

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder : RaygunErrorMessageBuilderBase
  {
    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      MonoTouchException mex = exception as MonoTouchException;
      if (mex != null && mex.NSException != null)
      {
        message.Message = mex.NSException.Reason;
        message.ClassName = mex.NSException.Name;
      }
      else
      {
        message.Message = exception.Message;
        message.ClassName = FormatTypeName(exceptionType, true);
      }

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

      #if !__UNIFIED__
      MonoTouchException mex = exception as MonoTouchException;
      if (mex != null && mex.NSException != null)
      {
        var ptr = Messaging.intptr_objc_msgSend(mex.NSException.Handle, Selector.GetHandle("callStackSymbols"));
        var arr = NSArray.StringArrayFromHandle(ptr);
        foreach (var line in arr)
        {
          lines.Add(new RaygunErrorStackTraceLineMessage { FileName = line });
        }
        return lines.ToArray();
      }
      #endif

      string stackTraceStr = exception.StackTrace;
      if (String.IsNullOrWhiteSpace(stackTraceStr))
      {
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

    private static RaygunErrorStackTraceLineMessage[] ParseStackTrace(string stackTrace)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      string[] stackTraceLines = stackTrace.Split('\n');
      foreach (string stackTraceLine in stackTraceLines)
      {
        try
        {
          int? lineNumber = null;
          string fileName = null;
          string methodName = null;
          string className = null;
          string stackTraceLn = stackTraceLine.Trim();

          // Line number
          int index = stackTraceLn.LastIndexOf(":", StringComparison.Ordinal);
          if (index > 0)
          {
            int lineNumberInteger;
            bool success = int.TryParse(stackTraceLn.Substring(index + 1), out lineNumberInteger);
            if (success)
            {
              lineNumber = lineNumberInteger;
              stackTraceLn = stackTraceLn.Substring(0, index);
            }
          }

          // File name
          index = stackTraceLn.LastIndexOf(" in ", StringComparison.Ordinal);
          if (index > 0)
          {
            fileName = stackTraceLn.Substring(index + 4);
            stackTraceLn = stackTraceLn.Substring(0, index);
          }

          // Method name
          if (!stackTraceLn.StartsWith("at (", StringComparison.Ordinal) && stackTraceLn.StartsWith("at ", StringComparison.Ordinal))
          {
            string parameters = "";
            int parameterStartIndex = stackTraceLn.LastIndexOf("(", StringComparison.Ordinal);
            if (parameterStartIndex > 0)
            {
              int parameterEndIndex = stackTraceLn.IndexOf("[0x", StringComparison.Ordinal) - 1;
              if (parameterEndIndex < 0)
              {
                parameterEndIndex = stackTraceLn.IndexOf("<0x", StringComparison.Ordinal) - 1;
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

            int methodStartIndex = stackTraceLn.IndexOf("<", StringComparison.Ordinal) + 1;
            int methodEndIndex = methodStartIndex;
            int methodSeparatorLength = 0;
            if (methodStartIndex > 0 && methodStartIndex < parameterStartIndex)
            {
              methodEndIndex = stackTraceLn.IndexOf(">", methodStartIndex, StringComparison.Ordinal) - 1;
              methodSeparatorLength = 2;
            }

            if (methodEndIndex - methodStartIndex <= 0)
            {
              methodStartIndex = stackTraceLn.LastIndexOf("..", parameterStartIndex, StringComparison.Ordinal) + 1;
              methodEndIndex = parameterStartIndex - 1;
              methodSeparatorLength = 1;
            }

            if (methodStartIndex <= 0)
            {
              methodStartIndex = stackTraceLn.LastIndexOf(".", parameterStartIndex, StringComparison.Ordinal) + 1;
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
            index = stackTraceLn.IndexOf("at ", StringComparison.Ordinal);
            if (index >= 0)
            {
              className = stackTraceLn.Substring(index + 3);
            }
          }

          var line = new RaygunErrorStackTraceLineMessage
          {
            FileName = fileName,
            LineNumber = lineNumber,
            MethodName = methodName,
            ClassName = className,
            Raw = stackTraceLine.Trim()
          };

          lines.Add(line);
        }
        catch
        {
          if (!String.IsNullOrWhiteSpace(stackTraceLine))
          {
            lines.Add(new RaygunErrorStackTraceLineMessage { Raw = stackTraceLine.Trim() });
          }
        }
      }
      
      return lines.ToArray();
    }
  }
}
