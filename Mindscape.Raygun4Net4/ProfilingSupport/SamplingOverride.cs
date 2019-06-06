
namespace Mindscape.Raygun4Net.ProfilingSupport
{
  public class UrlSamplingOverride : ISamplingOverride
  {
    public UrlSamplingOverride(string url, SamplingPolicy policy)
    {
      Url = url;
      Policy = policy;
    }

    public string Url { get; private set; }
    public SamplingPolicy Policy { get; private set; }
  }
}
