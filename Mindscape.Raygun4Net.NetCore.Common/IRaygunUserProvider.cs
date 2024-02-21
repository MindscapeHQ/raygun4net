#nullable enable

namespace Mindscape.Raygun4Net;

public interface IRaygunUserProvider
{
  public RaygunIdentifierMessage? GetUser();
}