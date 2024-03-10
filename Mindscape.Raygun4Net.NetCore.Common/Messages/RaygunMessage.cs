using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessage
  {
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    public RaygunMessageDetails Details { get; set; } = new();

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return $"[RaygunMessage: OccurredOn={OccurredOn}, Details={Details}]";
    }
  }
}