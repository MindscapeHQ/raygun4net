﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Mindscape.Raygun4Net.Diagnostics;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder : RaygunErrorMessageBuilderBase
  {
    private static readonly ConcurrentDictionary<string, PEDebugInformation> DebugInformationCache = new();
    public static Func<string, PEReader> AssemblyReaderProvider { get; set; } = PortableExecutableReaderExtensions.GetFileSystemPEReader;

    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      message.Message = exception.Message;
      message.ClassName = FormatTypeName(exceptionType, true);

      message.StackTrace = BuildStackTrace(exception);

      if (message.StackTrace != null)
      {
        // If we have a stack trace then grab the debug info images, and put them into an array
        // for the outgoing payload
        message.Images = GetDebugInfoForStackFrames(message.StackTrace).ToArray();
      }

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

      IList<Exception> innerExceptions = GetInnerExceptions(exception);
      if (innerExceptions != null && innerExceptions.Count > 0)
      {
        message.InnerErrors = new RaygunErrorMessage[innerExceptions.Count];
        int index = 0;
        foreach (Exception e in innerExceptions)
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

    private static IList<Exception> GetInnerExceptions(Exception exception)
    {
      AggregateException ae = exception as AggregateException;
      if (ae != null)
      {
        return ae.InnerExceptions;
      }

      ReflectionTypeLoadException rtle = exception as ReflectionTypeLoadException;
      if (rtle != null)
      {
        int index = 0;
        foreach (Exception e in rtle.LoaderExceptions)
        {
          try
          {
            e.Data["Type"] = rtle.Types[index];
          }
          catch
          {
          }

          index++;
        }

        return rtle.LoaderExceptions.ToList();
      }

      return null;
    }

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
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
          PEDebugInformation debugInfo = null;

          try
          {
            file = frame.GetFileName();
            lineNumber = frame.GetFileLineNumber();
            methodName = GenerateMethodName(method);
            className = method.ReflectedType != null ? method.ReflectedType.FullName : "(unknown)";
            ilOffset = frame.GetILOffset();
            debugInfo = TryGetDebugInformation(method.Module.FullyQualifiedName);

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
            ImageSignature = debugInfo?.Signature
          };

          lines.Add(line);
        }
      }

      return lines.ToArray();
    }

    private static IEnumerable<PEDebugInformation> GetDebugInfoForStackFrames(IEnumerable<RaygunErrorStackTraceLineMessage> frames)
    {
      if (DebugInformationCache.IsEmpty)
      {
        return Enumerable.Empty<PEDebugInformation>();
      }

      var imageMap = DebugInformationCache.Values.Where(x => x?.Signature != null).ToDictionary(k => k.Signature);
      var imageSet = new HashSet<PEDebugInformation>();

      foreach (var stackFrame in frames)
      {
        if (stackFrame.ImageSignature != null && imageMap.TryGetValue(stackFrame.ImageSignature, out var image))
        {
          imageSet.Add(image);
        }
      }

      return imageSet;
    }

    private static PEDebugInformation TryGetDebugInformation(string moduleName)
    {
      if (DebugInformationCache.TryGetValue(moduleName, out var cachedInfo))
      {
        return cachedInfo;
      }

      try
      {
        // Attempt to read out the Debug Info from the PE
        var peReader = AssemblyReaderProvider(moduleName);

        // If we got this far, the assembly/module exists, so whatever the result
        // put it in the cache to prevent reading the disk over and over
        peReader.TryGetDebugInformation(out var debugInfo);
        DebugInformationCache.TryAdd(moduleName, debugInfo);
        return debugInfo;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Could not load debug information: {ex}");
      }

      return null;
    }
  }
}