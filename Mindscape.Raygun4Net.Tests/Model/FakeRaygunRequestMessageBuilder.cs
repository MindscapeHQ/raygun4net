using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Tests
{
  public class FakeRaygunRequestMessageBuilder : RaygunRequestMessageBuilder
  {
    public static Dictionary<string, string> ExposeGetIgnoredFormValues(NameValueCollection form, Func<string, bool> ignore)
    {
      return GetIgnoredFormValues(form, ignore);
    }

    public static string ExposeStripIgnoredFormData(string rawData, Dictionary<string, string> ignored)
    {
      return StripIgnoredFormData(rawData, ignored);
    }
  }
}
