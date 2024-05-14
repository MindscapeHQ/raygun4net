using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public interface IOfflineErrorStore
{
  public Task<List<OfflineErrorRecord>> GetAll(CancellationToken cancellationToken);
  public Task<bool> Save(OfflineErrorRecord errorRecord, CancellationToken cancellationToken);
  public Task<bool> Remove(Guid errorId, CancellationToken cancellationToken);
}