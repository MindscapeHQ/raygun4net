#nullable enable

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Mindscape.Raygun4Net
{
  internal static class IpAddressMasker
  {
    private const int Ipv6PrefixLengthInBytes = 6;

    private static readonly string[] ClientIpAddressHeaderNames =
    {
      "CF-Connecting-IP",
      "Client-IP",
      "Fastly-Client-IP",
      "Fly-Client-IP",
      "Forwarded",
      "True-Client-IP",
      "X-Client-IP",
      "X-Cluster-Client-IP",
      "X-Forwarded",
      "X-Forwarded-For",
      "X-Original-Forwarded-For",
      "X-Real-IP"
    };

    /// <summary>
    /// Strict validation for accepting client addresses from headers / server variables
    /// (e.g. X-Forwarded-For, REMOTE_ADDR). IPv4 values must use four decimal octets without
    /// leading zeros. If a port is present, it must be numeric and in the range 0–65535.
    /// Invalid values are rejected so they do not override a valid REMOTE_ADDR.
    /// </summary>
    internal static bool IsValidAddress(string? address)
    {
      if (address == null || string.IsNullOrWhiteSpace(address) || address.Length != address.Trim().Length)
      {
        return false;
      }

      return TryParseAddress(address, requireCanonicalIpv4: true, requireValidPort: true, out _, out _, out _);
    }

    /// <summary>
    /// Best-effort masking for privacy. If the host portion is a parseable IP, it is masked even
    /// when an attached port/suffix is malformed (host is redacted; original suffix is kept).
    /// </summary>
    internal static string? Mask(string? address)
    {
      if (address == null || string.IsNullOrWhiteSpace(address))
      {
        return address;
      }

      string value = address.Trim();
      if (!TryParseAddress(value, requireCanonicalIpv4: false, requireValidPort: false, out IPAddress? ipAddress, out string prefix, out string suffix) ||
          ipAddress == null)
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

    internal static bool IsClientIpAddressHeader(string? name)
    {
      if (name == null || string.IsNullOrEmpty(name))
      {
        return false;
      }

      foreach (string headerName in ClientIpAddressHeaderNames)
      {
        if (headerName.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }

      return false;
    }

    internal static bool IsClientIpAddressServerVariable(string? name)
    {
      if (name == null || string.IsNullOrEmpty(name))
      {
        return false;
      }

      if (name.Equals("REMOTE_ADDR", StringComparison.OrdinalIgnoreCase) ||
          name.Equals("REMOTE_HOST", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      const string headerPrefix = "HTTP_";
      if (!name.StartsWith(headerPrefix, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      string headerName = name.Substring(headerPrefix.Length).Replace('_', '-');
      return IsClientIpAddressHeader(headerName);
    }

    private static bool TryParseAddress(string value, bool requireCanonicalIpv4, bool requireValidPort, out IPAddress? address, out string prefix, out string suffix)
    {
      address = null;
      prefix = string.Empty;
      suffix = string.Empty;

      if (string.IsNullOrWhiteSpace(value))
      {
        return false;
      }

      if (TryParseBracketedAddress(value, requireCanonicalIpv4, requireValidPort, out address, out suffix))
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

      if (TryParseUnbracketedAddress(value, requireCanonicalIpv4, out address))
      {
        return true;
      }

      return TryParseIpv4AddressWithPort(value, requireCanonicalIpv4, requireValidPort, out address, out suffix);
    }

    private static bool TryParseBracketedAddress(string value, bool requireCanonicalIpv4, bool requireValidPort, out IPAddress? address, out string suffix)
    {
      address = null;
      suffix = string.Empty;

      if (value.Length < 2 || value[0] != '[')
      {
        return false;
      }

      int closingBracket = value.IndexOf(']');
      if (closingBracket < 0 ||
          !IPAddress.TryParse(value.Substring(1, closingBracket - 1), out address) ||
          address == null ||
          (requireCanonicalIpv4 && address.AddressFamily != AddressFamily.InterNetworkV6))
      {
        address = null;
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

    private static bool TryParseUnbracketedAddress(string value, bool requireCanonicalIpv4, out IPAddress? address)
    {
      if (!IPAddress.TryParse(value, out address) || address == null)
      {
        address = null;
        return false;
      }

      if (requireCanonicalIpv4 &&
          address.AddressFamily == AddressFamily.InterNetwork &&
          !TryParseCanonicalIpv4Address(value, out address))
      {
        address = null;
        return false;
      }

      return true;
    }

    private static bool TryParseIpv4AddressWithPort(string value, bool requireCanonicalIpv4, bool requireValidPort, out IPAddress? address, out string suffix)
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
      string host = value.Substring(0, separator);
      bool isAddressParsed = requireCanonicalIpv4
        ? TryParseCanonicalIpv4Address(host, out address)
        : IPAddress.TryParse(host, out address);

      if (!isAddressParsed ||
          address == null ||
          address.AddressFamily != AddressFamily.InterNetwork)
      {
        address = null;
        return false;
      }

      suffix = portSuffix;
      return true;
    }

    private static bool TryParseCanonicalIpv4Address(string value, out IPAddress? address)
    {
      address = null;
      string[] components = value.Split('.');
      if (components.Length != 4)
      {
        return false;
      }

      byte[] bytes = new byte[4];
      for (int index = 0; index < components.Length; index++)
      {
        string component = components[index];
        if (component.Length == 0 ||
            (component.Length > 1 && component[0] == '0') ||
            !byte.TryParse(component, NumberStyles.None, CultureInfo.InvariantCulture, out bytes[index]))
        {
          return false;
        }
      }

      address = new IPAddress(bytes);
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
