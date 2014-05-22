using System;
using System.Collections.Generic;
using System.Text;

namespace Mindscape.Raygun4Net2.Tests
{
  public class WrapperException : Exception
  {
    public WrapperException(Exception innerException)
      : base("Something went wrong", innerException)
    {
    }
  }
}
