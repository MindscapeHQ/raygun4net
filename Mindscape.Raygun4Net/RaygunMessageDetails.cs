namespace Mindscape.Raygun4Net
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public RaygunErrorMessageDetails Error { get; set; }

    public RaygunRequestMessageDetails Request { get; set; }
  }
}