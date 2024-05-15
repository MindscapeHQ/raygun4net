namespace Mindscape.Raygun4Net
{
  public class RaygunErrorStackTraceLineMessage
  {
    public int LineNumber { get; set; }

    public string ClassName { get; set; }

    public string FileName { get; set; }

    public string MethodName { get; set; }

    public int ILOffset { get; set; }

    public int MethodToken { get; set; }
    
    public string PdbSignature { get; set; }
    
    public string PdbChecksum { get; set; }
    
    public string PdbFile { get; set; }
    
    public string PdbTimestamp { get; set; }
  }
}