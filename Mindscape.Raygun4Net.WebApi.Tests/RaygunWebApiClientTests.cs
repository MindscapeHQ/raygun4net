using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.WebApi.Tests.Model;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.WebApi.Tests
{
  [TestFixture]
  class RaygunWebApiClientTests
  {
    private FakeRaygunWebApiClient _client = new FakeRaygunWebApiClient();

    [Test]
    public void CanNotSendIfExcludingStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsFalse(_client.ExposeCanSend(message));
    }

    [Test]
    public void CanNotSendIfExcludingStatusCode_MultipleCodes()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "400, 404, 501";

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsFalse(_client.ExposeCanSend(message));
    }

    [Test]
    public void CanSendIfNotExcludingStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "400";

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsTrue(_client.ExposeCanSend(message));
    }

    [Test]
    public void CanSendIfNotExcludingAnyStatusCodes()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = null;

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails
        {
          Response = new RaygunResponseMessage
          {
            StatusCode = 404
          }
        }
      };

      Assert.IsTrue(_client.ExposeCanSend(message));
    }

    // this test is to make sure there is good null check coverage
    [Test]
    public void CanSendIfMessageIsNull()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = null;

      // Null message
      Assert.IsTrue(_client.ExposeCanSend(null));

      // Null message details
      Assert.IsTrue(_client.ExposeCanSend(new RaygunMessage()));

      RaygunMessage message = new RaygunMessage
      {
        Details = new RaygunMessageDetails()
      };

      // Null message response

      Assert.IsTrue(_client.ExposeCanSend(message));
    }

  }
}
