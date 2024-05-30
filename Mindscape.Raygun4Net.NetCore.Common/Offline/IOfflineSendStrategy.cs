using System;

namespace Mindscape.Raygun4Net.Offline;

public interface IOfflineSendStrategy : IDisposable
{
  public event Action OnSend;
  public void Start();
  public void Stop();
}