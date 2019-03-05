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

    public string Filter(string data, IList<string> sensitiveFields)
    {
      try
      {
        var doc = XDocument.Parse(data);

        FilterElementsRecursive(doc.Descendants(), sensitiveFields);

        return doc.ToString();
      }
      catch
      {
        return null;
      }
    }

    private void FilterElementsRecursive(IEnumerable<XElement> decendants, IList<string> sensitiveFields)
    {
      foreach (XElement element in decendants)
      {
        if (element.HasElements)
        {
          FilterElementsRecursive(element.Descendants(), sensitiveFields);
        }

        FilterElement(element, sensitiveFields);
      }
    }

    private void FilterElement(XElement element, IList<string> sensitiveFields)
    {
      if (sensitiveFields.Contains(element.Name.LocalName))
      {
        element.Value = FILTERED_VALUE;
      }
    }
  }
}
