﻿namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorStackTraceLineMessage
  {
    public int? LineNumber { get; set; }

    public string ClassName { get; set; }

    public string FileName { get; set; }

    public string MethodName { get; set; }

    public string Raw { get; set; }

    public int ILOffset { get; set; }

    public int MethodToken { get; set; }

    public string ImageSignature { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunErrorStackTraceLineMessage: LineNumber={0}, ClassName={1}, FileName={2}, MethodName={3}, Raw={4}, ILOffset={5}, MethodToken={6}, PdbSignature={7}]", 
                           LineNumber, ClassName, FileName, MethodName, Raw, ILOffset, MethodToken, ImageSignature);
    }
  }
}