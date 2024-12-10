#nullable enable

using System;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Interface that can be implemented to modify the RaygunMessage before it is sent to Raygun.
/// These should be registered as a Singleton in the DI container as the RaygunClient will only
/// use a single instance of each.
/// </summary>
public interface IMessageBuilder
{
  Task<RaygunMessage> Apply(RaygunMessage message, Exception exception);
}