#nullable enable

namespace Mindscape.Raygun4Net.AspNetCore;

internal sealed class NullRaygunUserProvider : IRaygunUserProvider
{
  public static NullRaygunUserProvider Instance { get; } = new();

  private NullRaygunUserProvider()
  {
  }

  public RaygunIdentifierMessage? GetUser()
  {
    return null;
  }
}
