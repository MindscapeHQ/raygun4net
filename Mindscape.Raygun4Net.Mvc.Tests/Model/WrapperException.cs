using System;

namespace Mindscape.Raygun4Net.Tests
{
  public class WrapperException : Exception
  {
    public WrapperException(Exception innerException)
      : base("Something went wrong", innerException)
    {
    }
  }
}
