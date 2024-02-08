using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return base.StripWrapperExceptions(exception);
    }
  }
}
