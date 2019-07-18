using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public interface IDataSampler
  {
    bool TakeSample(Uri uri);
  }
}
