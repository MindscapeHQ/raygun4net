using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public interface ISamplingManager
  {
    void SetSamplingPolicy(SamplingPolicy policy, List<UrlSamplingOverride> overrides = null);
    void AddOverride(UrlSamplingOverride samplingOverride);

    bool TakeSample(Uri uri);
  }
}
