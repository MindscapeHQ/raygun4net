namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public RaygunErrorMessage Error { get; set; }
#if !WINRT
    public RaygunRequestMessage Request { get; set; }
#endif

    public RaygunClientMessage Client { get; set; }
  }
}