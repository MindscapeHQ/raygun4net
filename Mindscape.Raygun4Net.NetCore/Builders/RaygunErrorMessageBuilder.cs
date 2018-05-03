using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunErrorMessageBuilder
  {
    private readonly RaygunErrorMessage _message;
    
    public static RaygunErrorMessageBuilder New()
    {
      return new RaygunErrorMessageBuilder();
    }

    public RaygunErrorMessageBuilder()
    {
      _message = new RaygunErrorMessage();
    }
    
    public RaygunErrorMessage Build(Exception exception)
    {
      var exceptionType = exception.GetType();

      _message.Message    = exception.Message;
      _message.ClassName  = FormatTypeName(exceptionType, true);
      _message.StackTrace = BuildStackTrace(exception);

      if (exception.Data != null)
      {
        IDictionary data = new Dictionary<object, object>();
        
        foreach (var key in exception.Data.Keys)
        {
          if (!RaygunClient.SentKey.Equals(key))
          {
            data[key] = exception.Data[key];
          }
        }
        
        _message.Data = data;
      }

      if (exception is AggregateException aggregate && aggregate.InnerExceptions != null)
      {
        var count = aggregate.InnerExceptions.Count;
        _message.InnerErrors = new RaygunErrorMessage[count];
    
        for (int i = 0; i < count; ++i)
        {
          _message.InnerErrors[i] = Build(aggregate.InnerExceptions[i]);
        }
      }
      else if (exception.InnerException != null)
      {
        _message.InnerError = Build(exception.InnerException);
      }

      return _message;
    }
    
    private string FormatTypeName(Type type, bool fullName)
    {
      string name = fullName ? type.FullName : type.Name;
      
      if (!type.IsConstructedGenericType)
      {
        return name;
      }

      var stringBuilder = new StringBuilder();
      
      stringBuilder.Append(name.Substring(0, name.IndexOf("`")));
      stringBuilder.Append("<");
      
      foreach (var argType in type.GenericTypeArguments)
      {
        stringBuilder.Append(FormatTypeName(argType, false)).Append(",");
      }
      
      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      stringBuilder.Append(">");

      return stringBuilder.ToString();
    }
    
    private RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
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

      foreach (var frame in frames)
      {
        var method = frame.GetMethod();

        if (method == null)
        {
          continue;
        }

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
          FileName   = file,
          LineNumber = lineNumber,
          MethodName = methodName,
          ClassName  = className
        };

        lines.Add(line);
      }

      return lines.ToArray();
    }
    
    private string GenerateMethodName(MethodBase method)
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
