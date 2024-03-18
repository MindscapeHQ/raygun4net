#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net.EnvironmentProviders;

public class EnvironmentVariablesProvider
{
  public static IDictionary? GetEnvironmentVariables(RaygunSettingsBase settings)
  {
    if (settings.EnvironmentVariables == null || settings.EnvironmentVariables?.Count == 0)
    {
      return null;
    }

    var result = new Dictionary<string, string>();
    var environmentVariables = Environment.GetEnvironmentVariables();

    foreach (var search in settings.EnvironmentVariables!)
    {
      if (Regex.IsMatch(search, "^[*]+$", RegexOptions.Compiled))
      {
        continue;
      }
      
      var values = search switch
      {
        _ when search.StartsWith("*") && search.EndsWith("*") => GetContainsVariable(environmentVariables, search.Trim('*')),
        _ when search.StartsWith("*") => GetEndsWithVariable(environmentVariables, search.Trim('*')),
        _ when search.EndsWith("*") => GetStartsWithVariable(environmentVariables, search.Trim('*')),
        _ => GetExactVariable(environmentVariables, search)
      };

      foreach (var (key, value) in values)
      {
        // If adding failed we probably already added it from a previous search
        if (!result.ContainsKey(key))
        {
          result.Add(key, value ?? string.Empty);
        }
      }
    }

    return result;
  }

  private static IEnumerable<(string, string?)> GetContainsVariable(IDictionary environmentVariables, string search)
  {
    var matchingKeys = environmentVariables.Keys.Cast<string>().Where(key => key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    
    return matchingKeys.Select(key => (key, environmentVariables[key]?.ToString()));
  }

  private static IEnumerable<(string, string?)> GetEndsWithVariable(IDictionary environmentVariables, string search)
  {
    var matchingKeys = environmentVariables.Keys.Cast<string>().Where(key => key.EndsWith(search, StringComparison.OrdinalIgnoreCase));
    
    return matchingKeys.Select(key => (key, environmentVariables[key]?.ToString()));
  }

  private static IEnumerable<(string, string?)> GetStartsWithVariable(IDictionary environmentVariables, string search)
  {
    var matchingKeys = environmentVariables.Keys.Cast<string>().Where(key => key.StartsWith(search, StringComparison.OrdinalIgnoreCase));
    
    return matchingKeys.Select(key => (key, environmentVariables[key]?.ToString()));
  }

  private static IEnumerable<(string, string?)> GetExactVariable(IDictionary environmentVariables, string search)
  {
    var matchingKey = environmentVariables.Keys.Cast<string>().FirstOrDefault(key => key.Equals(search, StringComparison.OrdinalIgnoreCase));
    
    if (matchingKey == null)
    {
      yield break;
    }
    
    yield return (matchingKey, environmentVariables[matchingKey]?.ToString());
  }
}