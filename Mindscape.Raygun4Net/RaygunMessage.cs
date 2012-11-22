using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessage
  {
    public RaygunMessage(Exception exception)
    {
      OccurredOn = DateTime.UtcNow;
      Details = new RaygunMessageDetails { Error = new RaygunErrorMessageDetails(exception) };
    }

    public DateTime OccurredOn { get; set; }

    public RaygunMessageDetails Details { get; set; }
  }
}