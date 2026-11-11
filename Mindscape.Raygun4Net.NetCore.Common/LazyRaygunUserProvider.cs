#nullable enable

using System;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the user provider on the first GetUser call, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// Use this when the user provider comes from a DI container and can depend on ILogger. It prevents a dependency cycle with
/// the Raygun logger. If the user provider cannot be created, the exception goes to the caller, and the next message tries
/// again. A message sent on the same thread while the user provider is being created, for example from a constructor that
/// logs an error, is sent without a user.
/// </remarks>
public sealed class LazyRaygunUserProvider : IRaygunUserProvider
{
  private readonly object _lock = new();
  private readonly Func<IRaygunUserProvider?> _getProvider;
  private IRaygunUserProvider? _provider;
  private bool _isCreated;

  /// <summary>
  /// Initializes a new instance of the LazyRaygunUserProvider.
  /// </summary>
  /// <param name="getProvider">Gets the user provider. It is not called again after it returns, and it can return null.</param>
  public LazyRaygunUserProvider(Func<IRaygunUserProvider?> getProvider)
  {
    _getProvider = getProvider;
  }

  public RaygunIdentifierMessage? GetUser()
  {
    return GetProvider()?.GetUser();
  }

  private IRaygunUserProvider? GetProvider()
  {
    // Re-entered while creating the user provider, from a constructor that logs an error
    if (Monitor.IsEntered(_lock))
    {
      return null;
    }

    lock (_lock)
    {
      // Null is a valid provider, so a flag records that it was created
      if (!_isCreated)
      {
        _provider = _getProvider();
        _isCreated = true;
      }

      return _provider;
    }
  }
}
