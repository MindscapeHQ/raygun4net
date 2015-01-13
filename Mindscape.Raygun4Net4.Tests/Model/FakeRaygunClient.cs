using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net;

namespace Mindscape.Raygun4Net4.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return base.StripWrapperExceptions(exception);
    }
  }
}
