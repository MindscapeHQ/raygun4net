namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunResponseMessage
  {
    public int StatusCode { get; set; }

    public string StatusDescription { get; set; }

    public string Content { get; set; }
  }
}
