using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Xamarin.Android.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient()
      : this("tempkey")
    {
    }

    public FakeRaygunClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunMessage ExposeBuildMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData);
    }

    public bool ExposeOnSendingMessage(RaygunMessage message)
    {
      return OnSendingMessage(message);
    }
  }
}
