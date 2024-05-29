using System;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Offline;

public interface IOfflineSendStrategy : IDisposable
{
  public event Action OnSend;
  void Start();
  void Stop();
}