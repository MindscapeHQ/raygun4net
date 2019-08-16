namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunImageMessage
  {
    public long BaseAddress { get; set; }

    public RaygunDebugInfoMessage[] DebugInfo { get; set; }
  }
}
