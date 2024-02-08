using System;

namespace Mindscape.Raygun4Net.WebApi.Tests.Model
{
  public class WrapperException : Exception
  {
    public WrapperException(Exception innerException)
      : base("Something went wrong", innerException)
    {
    }
  }
}
