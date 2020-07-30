﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
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

    public WebClient ExposeCreateWebClient()
    {
      return CreateWebClient();
    }

    public bool ExposeValidateApiKey()
    {
      return ValidateApiKey();
    }

    public bool ExposeOnSendingMessage(RaygunMessage raygunMessage, Exception exception)
    {
      return OnSendingMessage(raygunMessage, exception);
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
