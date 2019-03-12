using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Mindscape.Raygun4Net.Filters
{
  public class RaygunKeyValuePairDataFilter : IRaygunDataFilter
  {
    private const string FILTERED_VALUE = "[FILTERED]";

    public bool CanParse(string data)
    {
      return !string.IsNullOrEmpty(data) && data.Contains("=");
    }

    public string Filter(string data, IList<string> ignoredKeys)
    {
      try
      {
        var stringBuilder = new StringBuilder();

        // "key1=value1&key2=value2" => ["key1=value1", "key2=value2"]
        var kvps = data.Split('&');

        for (int i = 0; i < kvps.Length; ++i)
        {
          // "key1=value1" => ["key1", "value1"]
          var pair = kvps[i].Split('=');

          if (i > 0)
          {
            stringBuilder.Append("&");
          }

          if (ShouldIgnore(pair, ignoredKeys))
          {
            stringBuilder.Append(pair[0]);
            stringBuilder.Append("=");
            stringBuilder.Append(FILTERED_VALUE);
          }
          else
          {
            stringBuilder.Append(pair[0]);
            stringBuilder.Append("=");
            stringBuilder.Append(pair[1]);
          }
        }

        return stringBuilder.ToString();
      }
      catch
      {
        return null;
      }
    }

    private bool ShouldIgnore(string[] kvp, IList<string> ignoredKeys)
    {
      bool hasKey   = !string.IsNullOrWhiteSpace(kvp[0]);
      bool hasValue = !string.IsNullOrWhiteSpace(kvp[1]);

      bool isIgnoredKey = ignoredKeys.Any(f => f.Equals(kvp[0], StringComparison.OrdinalIgnoreCase));

      return hasKey && hasValue && isIgnoredKey;
    }
  }
}
