using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunClientBaseTests
  {
    private FakeRaygunClient _client = new FakeRaygunClient();

    [Test]
    public void FlagAsSentDoesNotCrash_DataDoesNotSupportStringKeys()
    {
      Assert.That(() => _client.ExposeFlagAsSent(new FakeException(new Dictionary<int, object>())), Throws.Nothing);
    }

    [Test]
    public void FlagAsSentDoesNotCrash_NullData()
    {
      Assert.That(() => _client.ExposeFlagAsSent(new FakeException(null)), Throws.Nothing);
    }

    [Test]
    public void CanSendIfDataIsNull()
    {
      Assert.That(_client.ExposeCanSend(new FakeException(null)), Is.True);
    }

    [Test]
    public void CannotSendSentException_StringDictionary()
    {
      Exception exception = new FakeException(new Dictionary<string, object>());
      _client.ExposeFlagAsSent(exception);
      Assert.That(_client.ExposeCanSend(exception), Is.False);
    }

    [Test]
    public void CannotSendSentException_ObjectDictionary()
    {
      Exception exception = new FakeException(new Dictionary<object, object>());
      _client.ExposeFlagAsSent(exception);
      Assert.That(_client.ExposeCanSend(exception), Is.False);
    }

    [Test]
    public void ExceptionInsideSendingMessageHAndlerDoesNotCrash()
    {
      FakeRaygunClient client = new FakeRaygunClient();
      client.SendingMessage += (sender, args) =>
      {
        throw new Exception("Oops...");
      };

      Assert.That(() => client.ExposeOnSendingMessage(new RaygunMessage()), Throws.Nothing);
      Assert.That(client.ExposeOnSendingMessage(new RaygunMessage()), Is.True);
    }
  }
}
