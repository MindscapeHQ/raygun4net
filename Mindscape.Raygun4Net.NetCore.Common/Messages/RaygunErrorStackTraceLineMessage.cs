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
  }
}