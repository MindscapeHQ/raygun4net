using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunRequestMessageOptionsTests
  {
    [Test]
    public void RequestIpAddressMaskingIsDisabledByDefault()
    {
      var options = new RaygunRequestMessageOptions();

      Assert.That(options.IsRequestIpAddressMasked, Is.False);
    }

    [Test]
    public void RequestIpAddressMaskingCanBeEnabled()
    {
      var options = new RaygunRequestMessageOptions
      {
        IsRequestIpAddressMasked = true
      };

      Assert.That(options.IsRequestIpAddressMasked, Is.True);
    }
  }
}
