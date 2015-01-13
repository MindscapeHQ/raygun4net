using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
