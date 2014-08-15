using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessage
  {
    public DateTime OccurredOn { get; set; }

    public RaygunMessageDetails Details { get; set; }
  }
}