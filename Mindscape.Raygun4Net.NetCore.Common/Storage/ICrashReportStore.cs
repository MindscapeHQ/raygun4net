using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public interface ICrashReportStore
{
  public Task<List<CrashReportStoreEntry>> GetAll(CancellationToken cancellationToken);
  public Task<CrashReportStoreEntry> Save(string crashPayload, string apiKey, CancellationToken cancellationToken);
  public Task<bool> Remove(Guid cacheEntryId, CancellationToken cancellationToken);
}