using System;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.AspNet5.Builders
{
  internal class RaygunEnvironmentMessageBuilder
  {
    internal static RaygunEnvironmentMessage Build()
    {
      return new RaygunEnvironmentMessage();
    }
  }
}