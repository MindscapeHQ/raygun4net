namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorStackTraceLineMessage
  {
    public int LineNumber { get; set; }

    public string ClassName { get; set; }

    public string FileName { get; set; }

    public string MethodName { get; set; }
  }
}