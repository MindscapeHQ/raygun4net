namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public RaygunErrorMessage Error { get; set; }

    public RaygunRequestMessage Request { get; set; }
  }
}