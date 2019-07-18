using System;

namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class UrlSamplingOverride : ISamplingOverride
  {
    public string Url { get; private set; }
    public SamplingPolicy Policy { get; private set; }

    public UrlSamplingOverride(string url, SamplingPolicy policy)
    {
      // We can't store 'url' in a 'System.Uri' because it's come from user input and
      // so it might not be in the correct format (i.e. new Uri(url) might throw!)
      Url = url;
      Policy = policy;
    }
  }
}
