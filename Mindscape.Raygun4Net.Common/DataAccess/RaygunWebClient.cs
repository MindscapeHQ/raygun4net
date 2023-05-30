using System;
using System.Net;
using System.Reflection;

namespace Mindscape.Raygun4Net.Common.DataAccess
{
#if !(NET40 || NET45)
  [Obsolete("Please migrate to using HttpClient, for non .net4.0 & .net4.5 code")]
#endif
  public class RaygunWebClient : WebClient
  {
    private readonly SecurityProtocolType _sslProtocol;


    private static readonly PropertyInfo SslProtocolsPropertyInfo =
      typeof(HttpWebRequest).GetProperty("SslProtocols", BindingFlags.Instance | BindingFlags.NonPublic);


    /// <summary>
    /// An override of System.Net.WebClient, which uses reflection to patch and enable TLS 1.2 and 1.3 in .net 4.0-4.5
    /// </summary>
    /// <param name="sslProtocol">The TLS protocols to use to make the network connection to Raygun. This should not normally be set, as correct defaults, for the given runtime, should be chosen automatically</param>
    public RaygunWebClient(SecurityProtocolType? sslProtocol = null)
    {
      SecurityProtocolType defaults = ServicePointManager.SecurityProtocol;

      _sslProtocol = sslProtocol ?? defaults;
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
      var request = base.GetWebRequest(address);

      if (request is HttpWebRequest)
      {
        SslProtocolsPropertyInfo.SetValue(request, _sslProtocol, null);
      }

      return request;
    }
  }
}