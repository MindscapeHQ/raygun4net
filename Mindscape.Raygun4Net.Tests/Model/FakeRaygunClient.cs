using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Tests
{
  public class FakeRaygunClient : RaygunClient
  {
    public FakeRaygunClient() { }

    public FakeRaygunClient(string apiKey)
      : base(apiKey)
    {
    }

    public RaygunMessage ExposeBuildMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData, [Optional] RaygunIdentifierMessage userIdentifierMessage)
    {
      return BuildMessage(exception, tags, userCustomData, userIdentifierMessage);
    }
    
    public IEnumerable<Exception> ExposeStripWrapperExceptions(Exception exception)
    {
      return StripWrapperExceptions(exception);
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
  }
}
