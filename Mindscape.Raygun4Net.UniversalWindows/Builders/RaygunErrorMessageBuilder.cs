using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunErrorMessageBuilder
  {
    private const int SIGNATURE_OFFSET_OFFSET = 60; // 0x3c
    private const int SIGNATURE_SIZE = 4;
    private const int COFF_FILE_HEADER_SIZE = 20;

    private const int DEBUG_DATA_DIRECTORY_OFFSET_32 = 144;
    private const int DEBUG_DATA_DIRECTORY_OFFSET_64 = 160;

    public static RaygunErrorMessage Build(Exception exception)
    {
      RaygunErrorMessage message = new RaygunErrorMessage();

      var exceptionType = exception.GetType();
      
      message.Message = exception.Message;
      message.ClassName = FormatTypeName(exceptionType, true);

      try
      {
        message.StackTrace = BuildStackTrace(exception);
      }
      catch (Exception e)
      {
        Debug.WriteLine(string.Format($"Failed to get managed stack trace information: {e.Message}"));
      }

      try
      {
        message.NativeStackTrace = BuildNativeStackTrace(exception);
      }
      catch (Exception e)
      {
        Debug.WriteLine(string.Format($"Failed to get native stack trace information: {e.Message}"));
      }

      message.Data = exception.Data;

      AggregateException ae = exception as AggregateException;
      if (ae?.InnerExceptions != null)
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

    private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorStackTraceLineMessage>();

      if (exception.StackTrace != null)
      {
        char[] separators = {'\r', '\n'};
        var frames = exception.StackTrace.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in frames)
        {
          // Trim the stack trace line
          string stackTraceLine = line.Trim();
          if (stackTraceLine.StartsWith("at "))
          {
            stackTraceLine = stackTraceLine.Substring(3);
          }

          RaygunErrorStackTraceLineMessage stackTraceLineMessage = new RaygunErrorStackTraceLineMessage
          {
            Raw = stackTraceLine
          };

          // Extract the method name and class name if possible:
          int index = stackTraceLine.IndexOf("(");
          if (index > 0)
          {
            index = stackTraceLine.LastIndexOf(".", index);
            if (index > 0)
            {
              stackTraceLineMessage.ClassName = stackTraceLine.Substring(0, index);
              stackTraceLineMessage.MethodName = stackTraceLine.Substring(index + 1);
            }
          }

          lines.Add(stackTraceLineMessage);
        }
      }

      return lines.Count == 0 ? null : lines.ToArray();
    }

    private static RaygunErrorNativeStackTraceLineMessage[] BuildNativeStackTrace(Exception exception)
    {
      var lines = new List<RaygunErrorNativeStackTraceLineMessage>();
      
      var stackTrace = new StackTrace(exception, true);
      var frames = stackTrace.GetFrames();

      foreach (StackFrame frame in frames)
      {
        if (frame.HasNativeImage())
        {
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
          //   Data directories (position 96/112)
          //     ...
          //     Debug (8 bytes, position 144/160) <-- I want this
          //     ...

          // TODO: use SizeOfOptionalHeader and NumberOfRvaAndSizes before tapping into data directories
          
          // All offset values are relative to the nativeImageBase
          int signatureOffset = CopyInt32(nativeImageBase + SIGNATURE_OFFSET_OFFSET);

          int optionalHeaderOffset = signatureOffset + SIGNATURE_SIZE + COFF_FILE_HEADER_SIZE;

          short magic = CopyInt16(nativeImageBase + optionalHeaderOffset);

          int sizeOfCode = CopyInt32(nativeImageBase + optionalHeaderOffset + 4);
          int baseOfCode = CopyInt32(nativeImageBase + optionalHeaderOffset + 20);

          int debugDataDirectoryOffset = optionalHeaderOffset + (magic == (short)PEMagic.PE32 ? DEBUG_DATA_DIRECTORY_OFFSET_32 : DEBUG_DATA_DIRECTORY_OFFSET_64);
          
          // TODO: this address can be 0 if there is no debug information:
          int debugVirtualAddress = CopyInt32(nativeImageBase + debugDataDirectoryOffset);
          
          int debugSize = CopyInt32(nativeImageBase + debugDataDirectoryOffset + 4);
          
          // A debug directory:
          
          int stamp = CopyInt32(nativeImageBase + debugVirtualAddress + 4);
          
          // TODO: check that this is 2
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

          var line = new RaygunErrorNativeStackTraceLineMessage
          {
            IP = nativeIP.ToInt64(),
            ImageBase = nativeImageBase.ToInt64()
          };
          
          lines.Add(line);
        }
      }

      return lines.Count == 0 ? null : lines.ToArray();
    }

    private static short CopyInt16(IntPtr address)
    {
      byte[] byteArray = new byte[2];
      Marshal.Copy(address, byteArray, 0, 2);
      return BitConverter.ToInt16(byteArray, 0);
    }

    private static int CopyInt32(IntPtr address)
    {
      byte[] byteArray = new byte[4];
      Marshal.Copy(address, byteArray, 0, 4);
      return BitConverter.ToInt32(byteArray, 0);
    }
  }
}
