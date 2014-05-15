namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunErrorStackTraceLineMessage
  {
    private int _lineNumber;
    private string _className;
    private string _fileName;
    private string _methodName;

    public int LineNumber
    {
      get { return _lineNumber; }
      set
      {
        _lineNumber = value;
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

    public string FileName
    {
      get { return _fileName; }
      set
      {
        _fileName = value;
      }
    }

    public string MethodName
    {
      get { return _methodName; }
      set
      {
        _methodName = value;
      }
    }
  }
}