using System;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessage
  {
    private DateTime _occurredOn;
    private RaygunMessageDetails _details;

    public RaygunMessage()
    {
      OccurredOn = DateTime.UtcNow;
      Details = new RaygunMessageDetails();
    }

    public DateTime OccurredOn
    {
      get { return _occurredOn; }
      set
      {
        _occurredOn = value;
      }
    }

    public RaygunMessageDetails Details
    {
      get { return _details; }
      set
      {
        _details = value;
      }
    }
  }
}