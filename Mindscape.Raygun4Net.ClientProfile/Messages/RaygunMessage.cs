using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessage
  {
    public RaygunMessage()
    {
      OccurredOn = DateTime.UtcNow;
      Details = new RaygunMessageDetails();
    }

    public DateTime OccurredOn { get; set; }

    public RaygunMessageDetails Details { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunMessage: OccurredOn={0}, Details={1}]", OccurredOn, Details);
    }
  }
}