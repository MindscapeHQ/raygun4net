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
        int lineNumber = 0;
        string fileName = null;
        string methodName = null;
        string className = null;
        string stackTraceLn = stackTraceLine;

        // Line number
        int index = stackTraceLine.LastIndexOf(":", StringComparison.Ordinal);
        if (index > 0)
        {
          bool success = int.TryParse(stackTraceLn.Substring(index + 1), out lineNumber);
          if (success)
          {
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
        index = stackTraceLn.LastIndexOf("(", StringComparison.Ordinal);
        if (index > 0 && !stackTraceLine.StartsWith("at (", StringComparison.Ordinal))
        {
          index = stackTraceLn.LastIndexOf(".", index, StringComparison.Ordinal);
          if (index > 0)
          {
            int endIndex = stackTraceLn.IndexOf("[0x", StringComparison.Ordinal);
            if (endIndex < 0)
            {
              endIndex = stackTraceLn.Length;
            }
            methodName = stackTraceLn.Substring(index + 1, endIndex - index - 1).Trim();
            methodName = methodName.Replace(" (", "(");
            stackTraceLn = stackTraceLn.Substring(0, index);

            // Memory address
            index = methodName.LastIndexOf("<0x", StringComparison.Ordinal);
            if (index >= 0)
            {
              methodName = methodName.Substring(0, index).Trim();
            }

            // Class name
            index = stackTraceLn.IndexOf("at ", StringComparison.Ordinal);
            if (index >= 0)
            {
              className = stackTraceLn.Substring(index + 3);
            }
          }
        }

        if (methodName == null && fileName == null)
        {
          if (!String.IsNullOrWhiteSpace(stackTraceLn) && stackTraceLn.StartsWith("at ", StringComparison.Ordinal))
          {
            stackTraceLn = stackTraceLn.Substring(3);
          }
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
      
      return lines.ToArray();
    }
  }
}
