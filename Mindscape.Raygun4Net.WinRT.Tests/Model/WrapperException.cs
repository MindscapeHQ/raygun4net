using System;

namespace Mindscape.Raygun4Net.WinRT.Tests
{
  public class WrapperException : Exception
  {
    public WrapperException(Exception innerException)
      : base("Something went wrong", innerException)
    {
    }
  }
}
