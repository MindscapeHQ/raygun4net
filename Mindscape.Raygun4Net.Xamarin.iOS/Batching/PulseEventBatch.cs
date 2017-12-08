using System;
using System.Collections.Generic;
using System.Threading;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  internal class PulseEventBatch
  {
    private List<PendingEvent> _pendingEvents = new List<PendingEvent>();
    private DateTime _lastUpdate;
    private readonly RaygunClient _raygunClient;
    private readonly RaygunIdentifierMessage _userInfo;

    private bool _locked;

    public PulseEventBatch(RaygunClient raygunClient)
    {
      _raygunClient = raygunClient;
      _userInfo = _raygunClient.UserInfo;
      _lastUpdate = DateTime.UtcNow;

      Thread t = new Thread(CheckTime);
      t.Start();
    }

    private void CheckTime()
    {
      while (true)
      {
        Thread.Sleep(1500);

        if ((DateTime.UtcNow - _lastUpdate).TotalSeconds > 1 && _pendingEvents.Count > 0)
        {
          Done();
          break;
        }
      }
    }

    public void Add(PendingEvent pendingEvent)
    {
      if (!_locked)
      {
        _lastUpdate = DateTime.UtcNow;
        _pendingEvents.Add(pendingEvent);
        if (_pendingEvents.Count >= 50)
        {
          Done();
        }
      }
    }

    public bool IsLocked
    {
      get { return _locked; }
    }

    public RaygunIdentifierMessage UserInfo
    {
      get { return _userInfo; }
    }

    public void Done()
    {
      if (!_locked)
      {
        _locked = true;
        _raygunClient.Send(this);
      }
    }

    public int PendingEventCount
    {
      get { return _pendingEvents.Count; }
    }

    public IEnumerable<PendingEvent> PendingEvents
    {
      get
      {
        foreach (PendingEvent pendingEvent in _pendingEvents)
        {
          yield return pendingEvent;
        }
      }
    }
  }

  internal class PendingEvent
  {
    private readonly RaygunPulseEventType _eventType;
    private readonly string _name;
    private readonly long _milliseconds;
    private readonly DateTime _timestamp;
    private readonly string _sessionId;

    public PendingEvent(RaygunPulseEventType eventType, string name, long milliseconds, string sessionId)
    {
      _eventType = eventType;
      _name = name;
      _milliseconds = milliseconds;
      _timestamp = DateTime.UtcNow - TimeSpan.FromMilliseconds(milliseconds);
      _sessionId = sessionId;
    }

    public RaygunPulseEventType EventType
    {
      get { return _eventType; }
    }

    public string Name
    {
      get { return _name; }
    }

    public long Duration
    {
      get { return _milliseconds; }
    }

    public DateTime Timestamp
    {
      get { return _timestamp; }
    }

    public string SessionId
    {
      get { return _sessionId; }
    }
  }
}
