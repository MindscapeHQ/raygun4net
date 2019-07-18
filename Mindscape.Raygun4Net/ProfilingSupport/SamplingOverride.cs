using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class UrlSamplingOverride : ISamplingOverride
  {
    public UrlSamplingOverride(string url, SamplingPolicy policy)
    {
      // Store as System.Uri becase that's what we use when we do the comparision (in SamplingManager)
      Url = new Uri(url); 
      Policy = policy;
    }

    public Uri Url { get; private set; }
    public SamplingPolicy Policy { get; private set; }
  }
}
