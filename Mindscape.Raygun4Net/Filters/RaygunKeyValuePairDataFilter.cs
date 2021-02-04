using System;
using System.Collections.Generic;
using System.Text;

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
          
          // Can occur when we only have a key and no value.
          if (pair.Length == 1)
          {
            stringBuilder.Append(pair[0]);
            continue;
          }
          else
          {
            stringBuilder.Append(pair[0]);
            stringBuilder.Append("=");
            stringBuilder.Append(ShouldIgnore(pair, ignoredKeys) ? FILTERED_VALUE : pair[1]);
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
      bool hasKey   = !string.IsNullOrEmpty(kvp[0]);
      bool hasValue = !string.IsNullOrEmpty(kvp[1]);

      return hasKey && hasValue && Contains(ignoredKeys, kvp[0]);
    }

    private bool Contains(IList<string> ignoredKeys, string key)
    {
      foreach (var ignoredKey in ignoredKeys)
      {
        if (ignoredKey.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }

      return false;
    }
  }
}
