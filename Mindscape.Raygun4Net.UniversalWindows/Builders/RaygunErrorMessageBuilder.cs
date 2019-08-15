using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder
  {
    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();

      if (string.IsNullOrWhiteSpace(exception.StackTrace))
      {
        message.Message = "StackTrace is null";
      }
      else
      {
        char[] delim = { '\r', '\n' };
        var frames = exception.StackTrace.Split(delim, StringSplitOptions.RemoveEmptyEntries);
        if (frames.Length == 0)
        {
          message.Message = "No frames";
        }
        else
        {
          message.Message = exception.StackTrace;

        }
      }
      
      message.ClassName = FormatTypeName(exceptionType, true);

      try
      {
        message.StackTrace = BuildStackTrace(message, exception);
      }
      catch (Exception e)
      {
        Debug.WriteLine(string.Format($"Failed to get native stack trace information: {e.Message}"));
      }

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

    private static string FormatTypeName(Type type, bool fullName)
    {
      string name = fullName ? type.FullName : type.Name;
      Type[] genericArguments = type.GenericTypeArguments;
      if (genericArguments.Length == 0)
      {
        return name;
      }

      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(name.Substring(0, name.IndexOf("`")));
      stringBuilder.Append("<");
      foreach (Type t in genericArguments)
      {
        stringBuilder.Append(FormatTypeName(t, false)).Append(",");
      }
      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      stringBuilder.Append(">");

      return stringBuilder.ToString();
    }

    /*private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      if (exception.StackTrace != null)
      {
        char[] delim = { '\r', '\n' };
        var frames = exception.StackTrace.Split(delim, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in frames)
        {
          // Trim the stack trace line
          string stackTraceLine = line.Trim();
          if (stackTraceLine.StartsWith("at "))
          {
            stackTraceLine = stackTraceLine.Substring(3);
          }

          string className = stackTraceLine;
          string methodName = null;

          // Extract the method name and class name if possible:
          int index = stackTraceLine.IndexOf("(");
          if (index > 0)
          {
            index = stackTraceLine.LastIndexOf(".", index);
            if (index > 0)
            {
              className = stackTraceLine.Substring(0, index);
              methodName = stackTraceLine.Substring(index + 1);
            }
          }

          RaygunErrorStackTraceLineMessage stackTraceLineMessage = new RaygunErrorStackTraceLineMessage();
          stackTraceLineMessage.ClassName = className;
          stackTraceLineMessage.MethodName = methodName;
          lines.Add(stackTraceLineMessage);
        }
      }

      return lines.ToArray();
    }*/

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(RaygunErrorMessage raygunErrorMessage, Exception exception)
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

      foreach (StackFrame frame in frames)
      {
        if (frame.HasNativeImage())
        {
          raygunErrorMessage.Message = "Has native image";
          IntPtr nativeIP = frame.GetNativeIP();
          IntPtr nativeImageBase = frame.GetNativeImageBase();

          // PE Format:
          // -----------
          // https://docs.microsoft.com/en-us/windows/win32/debug/pe-format
          // -----------
          // MS-DOS Stub
          // Signature (4 bytes, offset to this can be found at 0x3c)
          // COFF File Header (20 bytes)
          // Optional header (variable size)
          //   Standard fields
          //   Windows-specific fields
          //   Data directories (position 28/112)
          //     ...
          //     Debug (8 bytes, position 114/160) <-- I want this
          //     ...

          // TODO: use SizeOfOptionalHeader and NumberOfRvaAndSizes before tapping into data directories
          
          int signatureOffset = CopyInt32(nativeImageBase + 0x3c); // relative to image base

          // Assume 32 bit for now:
          int debugDirectoryEntriesOffset = signatureOffset + 4 + 20 + 144;
          
          int addressOfEntryPoint = CopyInt32(nativeImageBase + signatureOffset + 4 + 20 + 16);
          
          int baseOfCode = CopyInt32(nativeImageBase + signatureOffset + 4 + 20 + 20);
          
          int sizeOfCode = CopyInt32(nativeImageBase + signatureOffset + 4 + 20 + 4);
          
          int sizeOfImage = CopyInt32(nativeImageBase + signatureOffset + 4 + 20 + 56);


          // TODO: this address can be 0 if there is no debug information:
          int debugVirtualAddress = CopyInt32(nativeImageBase + debugDirectoryEntriesOffset);
          
          int debugSize = CopyInt32(nativeImageBase + debugDirectoryEntriesOffset + 4);
          
          // A debug directory:
          
          int stamp = CopyInt32(nativeImageBase + debugVirtualAddress + 4);
          
          int type = CopyInt32(nativeImageBase + debugVirtualAddress + 12);
          
          int sizeOfData = CopyInt32(nativeImageBase + debugVirtualAddress + 16);
          
          int addressOfRawData = CopyInt32(nativeImageBase + debugVirtualAddress + 20);
          
          int pointerToRawData = CopyInt32(nativeImageBase + debugVirtualAddress + 24);

          // Debug information:
          // Reference: http://www.godevtool.com/Other/pdb.htm

          // TODO: check that this is "RSDS" before looking into subsequent values
          int debugSignature = CopyInt32(nativeImageBase + addressOfRawData);

          byte[] debugGuidArray = new byte[16];
          Marshal.Copy(nativeImageBase + addressOfRawData + 4, debugGuidArray, 0, 16);

          // age

          byte[] fileNameArray = new byte[sizeOfData - 24];
          Marshal.Copy(nativeImageBase + addressOfRawData + 24, fileNameArray, 0, sizeOfData - 24);
          
          string pdbFileName = Encoding.UTF8.GetString(fileNameArray, 0, fileNameArray.Length);

          var line = new RaygunErrorStackTraceLineMessage
          {
            NativeIP = nativeIP.ToInt64().ToString(),
            NativeImageBase = nativeImageBase.ToInt64().ToString(),
            Temp = debugVirtualAddress + " " + debugSize + " " + stamp + " " + type + " " + sizeOfData + " " + addressOfRawData + " " + pointerToRawData + " " + debugSignature + " " + pdbFileName,
            Temp2 = addressOfEntryPoint + " " + baseOfCode + " " + sizeOfCode + " " + sizeOfImage
          };

          lines.Add(line);
        }

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

          string className = method.DeclaringType != null ? method.DeclaringType.FullName : "(unknown)";
        }
      }

      return lines.ToArray();
    }

    private static int CopyInt32(IntPtr address)
    {
      byte[] byteArray = new byte[4];
      Marshal.Copy(address, byteArray, 0, 4);
      return BitConverter.ToInt32(byteArray, 0);
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
  }
}
