using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient() : base(new RaygunSettings { ApiKey = string.Empty }, null, [])
    {
    }

    public FakeRaygunClient(string apiKey) : base(new RaygunSettings { ApiKey = apiKey }, null, [])
    {
    }

    public FakeRaygunClient(RaygunSettings settings) : base(settings, null, [])
    {
    }

    public RaygunMessage ExposeBuildMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData, [Optional] RaygunIdentifierMessage user)
    {
      var task = BuildMessage(exception, tags, userCustomData, user);

      task.Wait();

      return task.Result;
    }

    public bool ExposeValidateApiKey()
    {
      return ValidateApiKey();
    }

    public bool ExposeOnSendingMessage(RaygunMessage raygunMessage)
    {
      return OnSendingMessage(raygunMessage);
    }

    public bool ExposeCanSend(Exception exception)
    {
      return CanSend(exception);
    }

    public void ExposeFlagAsSent(Exception exception)
    {
      FlagAsSent(exception);
    }

    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return StripWrapperExceptions(exception);
    }
  }
}