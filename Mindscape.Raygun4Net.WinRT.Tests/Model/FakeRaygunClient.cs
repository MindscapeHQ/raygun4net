using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.WinRT.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient() { }

    public FakeRaygunClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunMessage ExposeBuildMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData);
    }

    public bool ExposeOnSendingMessage(RaygunMessage raygunMessage)
    {
      return OnSendingMessage(raygunMessage);
    }

    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return StripWrapperExceptions(exception);
    }
  }
}
