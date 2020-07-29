using System.Windows.Forms;

namespace Mindscape.Raygun4Net.Logging
{
  public interface IRaygunLogger
  {
    RaygunLogLevel LogLevel { get; set; }
    
    void Error(string message);
    
    void Warning(string message);
    
    void Info(string message);
    
    void Debug(string message);
    
    void Verbose(string message);
  }
}