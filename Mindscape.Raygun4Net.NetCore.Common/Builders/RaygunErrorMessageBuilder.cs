using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using Mindscape.Raygun4Net.Diagnostics;

namespace Mindscape.Raygun4Net
{
  public class RaygunErrorMessageBuilder
  {
    private static readonly ConcurrentDictionary<string, PdbDebugInformation> DebugInformationCache = new();
    public static Func<string, PEReader> AssemblyReaderProvider { get; set; } = PortableExecutableReaderExtensions.GetFileSystemPEReader;

    protected static string FormatTypeName(Type type, bool fullName)
    {
      string name = fullName ? type.FullName : type.Name;
      if (!type.IsConstructedGenericType)
      {
        return name;
      }

      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(name.Substring(0, name.IndexOf("`")));
      stringBuilder.Append("<");
      foreach (Type t in type.GenericTypeArguments)
      {
        stringBuilder.Append(FormatTypeName(t, false)).Append(",");
      }

      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      stringBuilder.Append(">");

      return stringBuilder.ToString();
    }

    protected static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var stackTrace = new StackTrace(exception, true);

      return BuildStackTrace(stackTrace);
    }

    public static RaygunErrorStackTraceLineMessage[] BuildStackTrace(StackTrace stackTrace)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      var frames = stackTrace.GetFrames();

      if (frames == null || frames.Length == 0)
      {
        var line = new RaygunErrorStackTraceLineMessage { FileName = "none", LineNumber = 0 };
        lines.Add(line);

        return lines.ToArray();
      }

      foreach (var frame in frames)
      {
        var method = frame.GetMethod();

        if (method != null)
        {
          string methodName = null;
          string file = null;
          string className = null;
          var lineNumber = 0;
          var ilOffset = StackFrame.OFFSET_UNKNOWN;
          var methodToken = StackFrame.OFFSET_UNKNOWN;
          PdbDebugInformation debugInfo = null;

          try
          {
            file = frame.GetFileName();
            lineNumber = frame.GetFileLineNumber();
            methodName = GenerateMethodName(method);
            className = method.ReflectedType != null ? method.ReflectedType.FullName : "(unknown)";
            ilOffset = frame.GetILOffset();

            // This might fail in medium trust environments or for array methods,
            // so don't crash the entire send process - just move on with what we have
            methodToken = method.MetadataToken;
          }
          catch (Exception ex)
          {
            Debug.WriteLine("Exception retrieving stack frame details: {0}", ex);
          }

          var line = new RaygunErrorStackTraceLineMessage
          {
            FileName = file,
            LineNumber = lineNumber,
            MethodName = methodName,
            ClassName = className,
            ILOffset = ilOffset,
            MethodToken = methodToken,
            PdbChecksum = debugInfo?.Checksum,
            PdbSignature = debugInfo?.Signature,
            PdbFile = debugInfo?.File,
            PdbTimestamp = debugInfo?.Timestamp
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

    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      message.Message = exception.Message;
      message.ClassName = FormatTypeName(exceptionType, true);

      message.StackTrace = BuildStackTrace(exception);

      if (exception.Data != null)
      {
        IDictionary data = new Dictionary<object, object>();

        foreach (object key in exception.Data.Keys)
        {
          if (!RaygunClientBase.SentKey.Equals(key))
          {
            data[key] = exception.Data[key];
          }
        }

        message.Data = data;
      }

      if (exception is AggregateException ae)
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

    private static PdbDebugInformation TryGetDebugInformation(string moduleName)
    {
      if (DebugInformationCache.TryGetValue(moduleName, out var cachedInfo))
      {
        return cachedInfo;
      }

      try
      {
        // Attempt to read out the Debug Info from the PE
        var peReader = AssemblyReaderProvider(moduleName);

        if (peReader.TryGetDebugInformation(out var debugInfo))
        {
          DebugInformationCache.TryAdd(moduleName, debugInfo);
          return debugInfo;
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Could not load debug information: {ex}");
      }

      return null;
    }
  }
}