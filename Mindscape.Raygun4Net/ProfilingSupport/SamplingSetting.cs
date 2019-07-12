using System.Collections.Generic;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class SamplingSetting
  {
    internal SamplingSetting(SamplingPolicy policy, List<UrlSamplingOverride> overrides)
    {
      Policy = policy;
      Overrides = overrides;
    }

    public SamplingPolicy Policy { get; }
    public List<UrlSamplingOverride> Overrides { get; }
  }
}
