#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the message builders on first use, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// Use this when the message builders come from a DI container and can depend on ILogger. It prevents a dependency cycle
/// with the Raygun logger. If a message builder cannot be created, the exception goes to the caller, and the next message
/// tries again. A message sent on the same thread while the message builders are being created, for example from a
/// constructor that logs an error, is sent without message builders.
/// </remarks>
public sealed class LazyMessageBuilders : IEnumerable<IMessageBuilder>
{
  private readonly object _lock = new();
  private readonly Func<IEnumerable<IMessageBuilder>> _getBuilders;
  private IEnumerable<IMessageBuilder>? _builders;

  /// <summary>
  /// Initializes a new instance of the LazyMessageBuilders.
  /// </summary>
  /// <param name="getBuilders">Gets the message builders. It is not called again after it returns.</param>
  public LazyMessageBuilders(Func<IEnumerable<IMessageBuilder>> getBuilders)
  {
    _getBuilders = getBuilders;
  }

  public IEnumerator<IMessageBuilder> GetEnumerator()
  {
    return GetBuilders().GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  private IEnumerable<IMessageBuilder> GetBuilders()
  {
    // Re-entered while creating the message builders, from a constructor that logs an error
    if (Monitor.IsEntered(_lock))
    {
      return Enumerable.Empty<IMessageBuilder>();
    }

    lock (_lock)
    {
      return _builders ??= _getBuilders();
    }
  }
}
