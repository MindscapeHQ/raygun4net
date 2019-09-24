using System;

namespace Mindscape.Raygun4Net.Attributes
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public sealed class DoNotProfileAttribute : Attribute
  {
  }
}