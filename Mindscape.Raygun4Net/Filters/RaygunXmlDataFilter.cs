using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Mindscape.Raygun4Net.Filters
{
  public class RaygunXmlDataFilter : IRaygunDataFilter
  {
    private const string FILTERED_VALUE = "[FILTERED]";

    public bool CanParse(string data)
    {
      if (!string.IsNullOrEmpty(data))
      {
        int index = data.TakeWhile(c => char.IsWhiteSpace(c)).Count();

        if (index < data.Length)
        {
          var firstChar = data.ElementAt(index);
          if (firstChar.Equals('<'))
          {
            return true;
          }
        }
      }

      return false;
    }

    public string Filter(string data, IList<string> ignoredKeys)
    {
      try
      {
        var doc = XDocument.Parse(data);

        // Begin the filtering.
        FilterElementsRecursive(doc.Descendants(), ignoredKeys);

        return doc.ToString();
      }
      catch
      {
        return null;
      }
    }

    private void FilterElementsRecursive(IEnumerable<XElement> decendants, IList<string> ignoredKeys)
    {
      foreach (XElement element in decendants)
      {
        if (element.HasElements)
        {
          // Keep searching for the outer leaf.
          FilterElementsRecursive(element.Descendants(), ignoredKeys);
        }

        // Remove sensitive values.
        FilterElement(element, ignoredKeys);
      }
    }

    private void FilterElement(XElement element, IList<string> ignoredKeys)
    {
      // Check if a value is set first and then if this element should be filtered.
      if (!string.IsNullOrEmpty(element.Value) && ignoredKeys.Any(f => f.Equals(element.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
      {
        element.Value = FILTERED_VALUE;
      }

      if (element.HasAttributes)
      {
        foreach (var attribute in element.Attributes())
        {
          // Check if a value is set first and then if this attribute should be filtered.
          if (!string.IsNullOrEmpty(attribute.Value) && ignoredKeys.Any(f => f.Equals(attribute.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
          {
            attribute.Value = FILTERED_VALUE;
          }
        }
      }
    }
  }
}
