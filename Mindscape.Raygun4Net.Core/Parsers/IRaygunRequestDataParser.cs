using System.Collections;

namespace Mindscape.Raygun4Net.Parsers
{
  public interface IRaygunRequestDataParser
  {
    IDictionary ToDictionary(string data);
  }
}
