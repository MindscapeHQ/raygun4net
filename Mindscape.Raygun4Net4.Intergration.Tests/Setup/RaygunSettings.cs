using System.Diagnostics.CodeAnalysis;

namespace Mindscape.Raygun4Net4.Integration.Tests.Setup
{
  [ExcludeFromCodeCoverage]
  internal class RaygunSettings
  {
    public string ApiKey { get; set; } = string.Empty;
  }
}
