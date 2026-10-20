#nullable enable

using System;
using System.Diagnostics;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the user provider on the first GetUser call, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// Use this when the user provider comes from a DI container and can depend on ILogger. It prevents a dependency cycle with
/// the Raygun logger. If the user provider cannot be created, the client sends messages without a user.
/// </remarks>
public sealed class LazyRaygunUserProvider : IRaygunUserProvider
{
  private readonly Lazy<IRaygunUserProvider?> _provider;

  /// <summary>
  /// Initializes a new instance of the LazyRaygunUserProvider.
  /// </summary>
  /// <param name="getProvider">Gets the user provider. It is called one time at most, and it can return null.</param>
  public LazyRaygunUserProvider(Func<IRaygunUserProvider?> getProvider)
  {
    _provider = new Lazy<IRaygunUserProvider?>(getProvider, LazyThreadSafetyMode.ExecutionAndPublication);
  }

  public RaygunIdentifierMessage? GetUser()
  {
    return GetProvider()?.GetUser();
  }

  private IRaygunUserProvider? GetProvider()
  {
    try
    {
      return _provider.Value;
    }
    catch (Exception ex)
    {
      // Lazy also throws when the constructor of the user provider logs an error on this thread
      Debug.WriteLine($"Raygun could not get the user provider, so this message is sent without a user: {ex}");
      return null;
    }
  }
}
