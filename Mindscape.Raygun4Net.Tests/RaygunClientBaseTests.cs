using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
      Assert.IsTrue(_client.ExposeCanSend(new FakeException(null)));
    }
  }
}
