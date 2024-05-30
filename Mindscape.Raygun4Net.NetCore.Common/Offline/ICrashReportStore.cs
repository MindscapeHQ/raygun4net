using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Offline;

public delegate Task SendHandler(string messagePayload, string apiKey, CancellationToken cancellationToken);

public interface ICrashReportStore
{
  public void SetSendCallback(SendHandler sendHandler);
  public Task<List<CrashReportStoreEntry>> GetAll(CancellationToken cancellationToken);
  public Task<bool> Save(string crashPayload, string apiKey, CancellationToken cancellationToken);
  public Task<bool> Remove(Guid cacheEntryId, CancellationToken cancellationToken);
}