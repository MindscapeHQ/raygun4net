#nullable enable

namespace Mindscape.Raygun4Net;

/// <summary>
/// Represents a provider for retrieving user information for Raygun error reporting.
/// </summary>
public interface IRaygunUserProvider
{
  /// <summary>
  /// Called when building the Error Report to include User Information associated with the error.
  /// </summary>
  RaygunIdentifierMessage? GetUser();
}