using System;
using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Parsers
{
  public class RaygunRequestDataJsonParser : IRaygunRequestDataParser
  {
    public IDictionary ToDictionary(string data)
    {
      try
      {
        return SimpleJson.DeserializeObject<Dictionary<string, string>>(data) as IDictionary;
      }
      catch
      {
        return null;
      }
    }
  }
}
