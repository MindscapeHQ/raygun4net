using System.Net;
using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net.AspNetCore;

internal static class HttpRequestExtensions
{
  /// <summary>
  /// Returns true if the IP address of the request originator was 127.0.0.1 or if the IP address of the request was the same as the server's IP address.
  /// </summary>
  /// <remarks>
  /// Credit to Filip W for the initial implementation of this method.
  /// See http://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/
  /// </remarks>
  public static bool IsLocal(this HttpRequest req)
  {
    var connection = req.HttpContext.Connection;
    if (connection.RemoteIpAddress != null)
    {
      return (connection.LocalIpAddress != null && connection.RemoteIpAddress.Equals(connection.LocalIpAddress)) || IPAddress.IsLoopback(connection.RemoteIpAddress);
    }

    // for in memory TestServer or when dealing with default connection info
    if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
    {
      return true;
    }

    return false;
  }
}