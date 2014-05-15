using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorMessage
  {
    private RaygunErrorMessage _innerError;
    private IDictionary _data;
    private string _className;
    private string _message;
    private RaygunErrorStackTraceLineMessage[] _stackTrace;

    public RaygunErrorMessage()
    {
    }

    public RaygunErrorMessage(string message, string stackTrace, string type)
    {
      Message = message;
      ClassName = type ?? "Exception";

      StackTrace = BuildStackTrace(stackTrace);
    }

    public RaygunErrorMessage(Exception exception)
    {
      Type exceptionType = exception.GetType();

      Message = string.Format("{0}: {1}", exceptionType.Name, exception.Message);
      ClassName = exceptionType.FullName;

      StackTrace = BuildStackTrace(exception);
      Data = exception.Data;

      if (exception.InnerException != null)
      {
        InnerError = new RaygunErrorMessage(exception.InnerException);
      }
    }

    private RaygunErrorStackTraceLineMessage[] BuildStackTrace(string stackTrace)
    {
      List<RaygunErrorStackTraceLineMessage> lines = new List<RaygunErrorStackTraceLineMessage>();

      return lines.ToArray();
    }

    private RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      List<RaygunErrorStackTraceLineMessage> lines = new List<RaygunErrorStackTraceLineMessage>();

      string stackTraceStr = exception.StackTrace;
      if (stackTraceStr == null)
      {
        RaygunErrorStackTraceLineMessage line = new RaygunErrorStackTraceLineMessage();
        line.FileName = "none";
        line.LineNumber = 0;

        lines.Add(line);
        return lines.ToArray();
      }
      try
      {
        string[] stackTraceLines = stackTraceStr.Split('\r', '\n');
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
              if (success)
              {
                stackTraceLn = stackTraceLn.Substring(0, index);
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
                  // Method name
                  index = stackTraceLn.LastIndexOf("(");
                  if (index > 0)
                  {
                    index = stackTraceLn.LastIndexOf(".", index);
                    if (index > 0)
                    {
                      methodName = stackTraceLn.Substring(index + 1).Trim();
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
            RaygunErrorStackTraceLineMessage line = new RaygunErrorStackTraceLineMessage();
            line.FileName = fileName;
            line.LineNumber = lineNumber;
            line.MethodName = methodName;
            line.ClassName = className;

            lines.Add(line);
          }
        }
        if (lines.Count > 0)
        {
          return lines.ToArray();
        }
      }
      catch { }



      StackTrace stackTrace = new StackTrace(exception, true);
      StackFrame[] frames = stackTrace.GetFrames();

      if (frames == null || frames.Length == 0)
      {
        RaygunErrorStackTraceLineMessage line = new RaygunErrorStackTraceLineMessage();
        line.FileName = "none";
        line.LineNumber = 0;

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

          string methodName = GenerateMethodName(method);

          string file = frame.GetFileName();

          string className = method.ReflectedType != null
                       ? method.ReflectedType.FullName
                       : "(unknown)";

          RaygunErrorStackTraceLineMessage line = new RaygunErrorStackTraceLineMessage();
          line.FileName = file;
          line.LineNumber = lineNumber;
          line.MethodName = methodName;
          line.ClassName = className;

          lines.Add(line);
        }
      }

      return lines.ToArray();
    }

    private string GenerateMethodName(MethodBase method)
    {
      StringBuilder stringBuilder = new StringBuilder();

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

    public RaygunErrorMessage InnerError
    {
      get { return _innerError; }
      set
      {
        _innerError = value;
      }
    }

    public IDictionary Data
    {
      get { return _data; }
      set
      {
        _data = value;
      }
    }

    public string ClassName
    {
      get { return _className; }
      set
      {
        _className = value;
      }
    }

    public string Message
    {
      get { return _message; }
      set
      {
        _message = value;
      }
    }

    public RaygunErrorStackTraceLineMessage[] StackTrace
    {
      get { return _stackTrace; }
      set
      {
        _stackTrace = value;
      }
    }
  }
}
