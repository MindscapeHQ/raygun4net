using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global

[assembly: InternalsVisibleTo("Mindscape.Raygun4Net.Modern.Tests")]

namespace Mindscape.Raygun4Net.Testable
{
  internal class TestableRaygunClient: RaygunClient
  {
    public TestableRaygunClient()
    {
    }

    public TestableRaygunClient(string apiKey)
      : base(apiKey)
    {
    }
  }
}