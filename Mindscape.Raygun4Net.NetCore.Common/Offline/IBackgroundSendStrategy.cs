using System;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Offline;

public interface IBackgroundSendStrategy : IDisposable
{
  public event Func<Task> OnSendAsync;
  public void Start();
  public void Stop();
}