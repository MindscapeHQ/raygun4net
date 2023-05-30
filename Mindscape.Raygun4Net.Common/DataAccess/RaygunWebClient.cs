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

#if NET40 || NET45
    private static readonly PropertyInfo SslProtocolsPropertyInfo =
      typeof(HttpWebRequest).GetProperty("SslProtocols", BindingFlags.Instance | BindingFlags.NonPublic);
#endif

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sslProtocol">The tls protocols to use to make the network connection to Raygun. This should not normally be set, as correct defaults, for the given runtime, should be chosen automatically</param>
    public RaygunWebClient(SecurityProtocolType? sslProtocol = null)
    {
      SecurityProtocolType defaults = ServicePointManager.SecurityProtocol;
//#if NET40
      defaults |= (SecurityProtocolType)3072; //TLS 1.2
      defaults |= (SecurityProtocolType) 12288; //TLS 1.3
//#elif NET45
//      defaults |= SecurityProtocolType.Tls12;
//      defaults |= (SecurityProtocolType) 12288; //TLS 1.3
//#endif
      _sslProtocol = sslProtocol ?? defaults;
    }

//#if NET40 || NET45
    protected override WebRequest GetWebRequest(Uri address)
    {
      var request = base.GetWebRequest(address);

      if (request is HttpWebRequest)
      {
        SslProtocolsPropertyInfo.SetValue(request, _sslProtocol, null);
      }

      return request;
    }
//#endif
  }
}