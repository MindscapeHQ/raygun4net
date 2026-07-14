using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Mindscape.Raygun4Net
{
  internal static class IpAddressMasker
  {
    private const int Ipv6PrefixLengthInBytes = 6;

    /// <summary>
    /// Strict validation for accepting client addresses from headers / server variables
    /// (e.g. X-Forwarded-For, REMOTE_ADDR). Requires a parseable IP and, if a port is present,
    /// a numeric port in the range 0–65535. Invalid port suffixes are rejected so they do not
    /// override a valid REMOTE_ADDR.
    /// </summary>
    internal static bool IsValidAddress(string address)
    {
      return TryParseAddress(address, requireValidPort: true, out _, out _, out _);
    }

    /// <summary>
    /// Best-effort masking for privacy. If the host portion is a parseable IP, it is masked even
    /// when an attached port/suffix is malformed (host is redacted; original suffix is kept).
    /// </summary>
    internal static string Mask(string address)
    {
      if (string.IsNullOrWhiteSpace(address))
      {
        return address;
      }

      string value = address.Trim();
      if (!TryParseAddress(value, requireValidPort: false, out IPAddress ipAddress, out string prefix, out string suffix))
      {
        return address;
      }

      return prefix + Mask(ipAddress) + suffix;
    }

    internal static IPAddress Mask(IPAddress address)
    {
      if (address == null)
      {
        throw new ArgumentNullException(nameof(address));
      }

      if (address.IsIPv4MappedToIPv6)
      {
        return Mask(address.MapToIPv4()).MapToIPv6();
      }

      byte[] bytes = address.GetAddressBytes();

      if (address.AddressFamily == AddressFamily.InterNetwork)
      {
        bytes[bytes.Length - 1] = 0;
        return new IPAddress(bytes);
      }

      if (address.AddressFamily == AddressFamily.InterNetworkV6)
      {
        Array.Clear(bytes, Ipv6PrefixLengthInBytes, bytes.Length - Ipv6PrefixLengthInBytes);
        return new IPAddress(bytes, address.ScopeId);
      }

      return address;
    }

    private static bool TryParseAddress(string value, bool requireValidPort, out IPAddress address, out string prefix, out string suffix)
    {
      address = null;
      prefix = string.Empty;
      suffix = string.Empty;

      if (string.IsNullOrWhiteSpace(value))
      {
        return false;
      }

      if (TryParseBracketedAddress(value, requireValidPort, out address, out suffix))
      {
        prefix = "[";
        return true;
      }

      // Bracketed forms are only handled above. Do not fall through to IPAddress.TryParse:
      // on modern .NET it accepts "[ipv6]:port" (including invalid ports), which would bypass
      // strict port validation used for X-Forwarded-For / REMOTE_ADDR acceptance.
      if (value.Length >= 2 && value[0] == '[')
      {
        return false;
      }

      if (IPAddress.TryParse(value, out address))
      {
        return true;
      }

      return TryParseIpv4AddressWithPort(value, requireValidPort, out address, out suffix);
    }

    private static bool TryParseBracketedAddress(string value, bool requireValidPort, out IPAddress address, out string suffix)
    {
      address = null;
      suffix = string.Empty;

      if (value.Length < 2 || value[0] != '[')
      {
        return false;
      }

      int closingBracket = value.IndexOf(']');
      if (closingBracket < 0 ||
          !IPAddress.TryParse(value.Substring(1, closingBracket - 1), out address))
      {
        return false;
      }

      string remainder = value.Substring(closingBracket + 1);
      if (remainder.Length > 0 && remainder[0] != ':')
      {
        address = null;
        return false;
      }

      if (requireValidPort && !IsValidPortSuffix(remainder))
      {
        address = null;
        return false;
      }

      suffix = "]" + remainder;
      return true;
    }

    private static bool TryParseIpv4AddressWithPort(string value, bool requireValidPort, out IPAddress address, out string suffix)
    {
      address = null;
      suffix = string.Empty;

      int separator = value.LastIndexOf(':');
      // Exactly one colon so we do not confuse IPv6 with IPv4:port.
      if (separator <= 0 || value.IndexOf(':') != separator)
      {
        return false;
      }

      string portSuffix = value.Substring(separator);

      if (requireValidPort && !IsValidPortSuffix(portSuffix))
      {
        return false;
      }

      // Host portion must be IPv4. For masking (requireValidPort == false), the port/suffix may be
      // invalid, empty, or non-numeric — still parse so the host can be redacted.
      if (!IPAddress.TryParse(value.Substring(0, separator), out address) ||
          address.AddressFamily != AddressFamily.InterNetwork)
      {
        address = null;
        return false;
      }

      suffix = portSuffix;
      return true;
    }

    private static bool IsValidPortSuffix(string suffix)
    {
      if (suffix.Length == 0)
      {
        return true;
      }

      int port;
      return suffix[0] == ':' &&
             int.TryParse(suffix.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out port) &&
             port >= 0 &&
             port <= ushort.MaxValue;
    }
  }
}
