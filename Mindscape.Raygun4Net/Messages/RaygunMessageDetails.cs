namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public RaygunErrorMessage Error { get; set; }
#if !WINRT
    public RaygunRequestMessage Request { get; set; }
#endif

    public RaygunEnvironmentMessage Environment { get; set; }

    public RaygunClientMessage Client { get; set; }
  }
}