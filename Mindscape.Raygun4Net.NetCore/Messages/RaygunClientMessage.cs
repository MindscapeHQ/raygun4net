namespace Mindscape.Raygun4Net
{
  public class RaygunClientMessage
  {
    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
    
    public RaygunClientMessage()
    {
      Name = "Raygun4Net.NetCore";
      Version = "5.6.0";
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }
  }
}