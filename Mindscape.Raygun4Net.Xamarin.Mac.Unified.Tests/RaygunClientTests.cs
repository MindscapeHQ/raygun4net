using System;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Xamarin.Mac.Unified.Tests
{
  [TestFixture]
  public class RaygunClientTests
  {
    private FakeRaygunClient _client;
    private Exception _exception = new NullReferenceException("The thing is null");

    [SetUp]
    public void SetUp()
    {
      _client = new FakeRaygunClient();
    }

    // Cancel send tests

    [Test]
    public void NoHandlerSendsAll()
    {
      Assert.IsTrue(_client.ExposeOnSendingMessage(_client.ExposeBuildMessage(_exception)));
    }
  }
}

