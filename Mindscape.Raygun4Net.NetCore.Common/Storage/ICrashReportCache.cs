using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public interface ICrashReportCache
{
  public Task<List<CrashReportCacheEntry>> GetAll(CancellationToken cancellationToken);
  public Task<CrashReportCacheEntry> Save(string crashPayload, string apiKey, CancellationToken cancellationToken);
  public Task<bool> Remove(CrashReportCacheEntry cacheEntry, CancellationToken cancellationToken);
}