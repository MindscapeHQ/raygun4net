#nullable enable

using System;
using System.Diagnostics;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the user provider on the first GetUser call, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// This prevents a dependency cycle when the user provider depends on ILogger and the Raygun logger is registered.
/// </remarks>
internal sealed class LazyRaygunUserProvider : IRaygunUserProvider
{
  private readonly Lazy<IRaygunUserProvider?> _provider;

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
