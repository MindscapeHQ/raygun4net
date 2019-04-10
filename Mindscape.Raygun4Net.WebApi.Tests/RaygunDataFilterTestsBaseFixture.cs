using System.IO;
using System.Reflection;

namespace Mindscape.Raygun4Net.WebApi.Tests
{
  public class RaygunDataFilterTestsBaseFixture
  {
    protected string LoadPayload(string resourceName)
    {
      var assembly = Assembly.GetExecutingAssembly();
      var resource = "Mindscape.Raygun4Net.WebApi.Tests.Payloads." + resourceName;

      string result = null;

      using (Stream stream = assembly.GetManifestResourceStream(resource))
      using (var reader = new StreamReader(stream))
      {
        result = reader.ReadToEnd();
      }

      return result;
    }
  }
}
