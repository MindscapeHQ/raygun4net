using System.Net;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

[TestFixture]
public class IpAddressMaskerTests
{
  [TestCase("", "")]
  [TestCase("not-an-ip-address", "not-an-ip-address")]
  [TestCase("192.168.12.123", "192.168.12.0")]
  [TestCase("192.168.12.123:8080", "192.168.12.0:8080")]
  // Invalid / non-numeric ports: still mask the host for privacy
  [TestCase("192.168.12.123:65536", "192.168.12.0:65536")]
  [TestCase("192.168.12.123:abc", "192.168.12.0:abc")]
  [TestCase("192.168.12.123:", "192.168.12.0:")]
  [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", "2001:db8:1234::")]
  [TestCase("[2001:db8:1234:5678:9abc:def0:1234:5678]:443", "[2001:db8:1234::]:443")]
  [TestCase("[2001:db8:1234:5678:9abc:def0:1234:5678]:65536", "[2001:db8:1234::]:65536")]
  [TestCase("::ffff:192.168.12.123", "::ffff:192.168.12.0")]
  public void MaskHandlesSupportedAddressFormats(string address, string expected)
  {
    IpAddressMasker.Mask(address).Should().Be(expected);
  }

  [TestCase("", false)]
  [TestCase("not-an-ip-address", false)]
  [TestCase("192.168.12.123", true)]
  [TestCase("192.168.12.123:8080", true)]
  [TestCase("192.168.12.123:0", true)]
  [TestCase("192.168.12.123:65535", true)]
  // Invalid ports must not pass strict validation (XFF should not override REMOTE_ADDR)
  [TestCase("192.168.12.123:65536", false)]
  [TestCase("192.168.12.123:abc", false)]
  [TestCase("192.168.12.123:", false)]
  [TestCase("2001:db8:1234:5678:9abc:def0:1234:5678", true)]
  [TestCase("[2001:db8:1234:5678:9abc:def0:1234:5678]", true)]
  [TestCase("[2001:db8:1234:5678:9abc:def0:1234:5678]:443", true)]
  [TestCase("[2001:db8:1234:5678:9abc:def0:1234:5678]:65536", false)]
  [TestCase("::ffff:192.168.12.123", true)]
  [TestCase("[2001:db8::1]junk", false)]
  public void IsValidAddressAcceptsOnlyStrictAddressForms(string address, bool expected)
  {
    IpAddressMasker.IsValidAddress(address).Should().Be(expected);
  }

  [Test]
  public void IsValidAddressRejectsNull()
  {
    IpAddressMasker.IsValidAddress(null!).Should().BeFalse();
  }

  [Test]
  public void MaskPreservesNull()
  {
    IpAddressMasker.Mask((string)null!).Should().BeNull();
  }

  [Test]
  public void MaskPreservesIpv6ScopeId()
  {
    var address = IPAddress.Parse("fe80::1234%7");

    var maskedAddress = IpAddressMasker.Mask(address);

    maskedAddress.ToString().Should().Be("fe80::%7");
    maskedAddress.ScopeId.Should().Be(7);
  }
}
