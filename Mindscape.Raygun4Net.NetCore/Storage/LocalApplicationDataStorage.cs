using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public sealed class LocalApplicationDataStorage : IOfflineErrorStore
{
  private readonly FileSystemErrorStore _fileSystemErrorStorage;

  public LocalApplicationDataStorage(string uniqueApplicationId = null)
  {
    var uniqueId = uniqueApplicationId ?? Assembly.GetEntryAssembly()?.Location ?? throw new ApplicationException("Cannot determine unique id");
    var uniqueIdHash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(uniqueId));
    var uniqueIdDirectory = BitConverter.ToString(uniqueIdHash).Replace("-", "").ToLowerInvariant();

    var localAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), uniqueIdDirectory);
    _fileSystemErrorStorage = new FileSystemErrorStore(localAppDirectory);
  }

  public Task<List<OfflineErrorRecord>> GetAll(CancellationToken cancellationToken)
  {
    return _fileSystemErrorStorage.GetAll(cancellationToken);
  }

  public Task<bool> Save(OfflineErrorRecord errorRecord, CancellationToken cancellationToken)
  {
    return _fileSystemErrorStorage.Save(errorRecord, cancellationToken);
  }

  public Task<bool> Remove(Guid errorId, CancellationToken cancellationToken)
  {
    return _fileSystemErrorStorage.Remove(errorId, cancellationToken);
  }
}