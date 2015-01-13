using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
