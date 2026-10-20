#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the message builders on first use, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// Use this when the message builders come from a DI container and can depend on ILogger. It prevents a dependency cycle
/// with the Raygun logger. If a message builder cannot be created, the client sends messages without message builders.
/// </remarks>
public sealed class LazyMessageBuilders : IEnumerable<IMessageBuilder>
{
  private readonly Lazy<IEnumerable<IMessageBuilder>> _builders;

  /// <summary>
  /// Initializes a new instance of the LazyMessageBuilders.
  /// </summary>
  /// <param name="getBuilders">Gets the message builders. It is called one time at most.</param>
  public LazyMessageBuilders(Func<IEnumerable<IMessageBuilder>> getBuilders)
  {
    _builders = new Lazy<IEnumerable<IMessageBuilder>>(getBuilders, LazyThreadSafetyMode.ExecutionAndPublication);
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
    try
    {
      return _builders.Value;
    }
    catch (Exception ex)
    {
      // Lazy also throws when the constructor of a message builder logs an error on this thread
      Debug.WriteLine($"Raygun could not get the message builders, so this message is sent without them: {ex}");
      return Enumerable.Empty<IMessageBuilder>();
    }
  }
}
