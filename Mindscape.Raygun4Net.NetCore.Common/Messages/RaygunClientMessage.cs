namespace Mindscape.Raygun4Net
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = "Raygun4Net.NetCore";
      Version = "6.2.0";
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }
    
    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}