using System.Collections;

namespace Mindscape.Raygun4Net.Messages
{
  public interface IRaygunRequestMessage
  {
    string HostName { get; set; }

    string Url { get; set; }

    string HttpMethod { get; set; }

    string IPAddress { get; set; }

    string RawData { get; set; }

    IDictionary Headers { get; set; }
  }
}