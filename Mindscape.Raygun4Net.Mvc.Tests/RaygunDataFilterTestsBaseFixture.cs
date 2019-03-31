using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  public class RaygunDataFilterTestsBaseFixture
  {
    protected string LoadResource(string resourceName)
    {
      var stack = new StackTrace();

      foreach (var frame in stack.GetFrames())
      {
        if (frame.GetMethod().ReflectedType.Namespace.Contains("Mindscape.Raygun4Net.Mvc.Tests"))
        {
          using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Concat(frame.GetMethod().ReflectedType.Namespace, ".Payloads.", resourceName))))
          {
            return reader.ReadToEnd();
          }
        }
      }

      throw new ArgumentException("Could not find resource " + resourceName);
    }
  }
}
