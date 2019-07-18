using System.Collections.Generic;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SamplingSetting
  {
    public SamplingPolicy Policy { get; }
    public List<UrlSamplingOverride> Overrides { get; }

    internal SamplingSetting(SamplingPolicy policy, List<UrlSamplingOverride> overrides)
    {
      Policy = policy;
      Overrides = overrides;
    }
  }
}
