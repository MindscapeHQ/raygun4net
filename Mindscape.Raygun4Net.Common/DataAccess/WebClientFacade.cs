

using System;
using System.Net;

namespace Mindscape.Raygun4Net.Common.DataAccess
{
  public class WebClientFacade : IHttpClient, IDisposable
  {
    private readonly WebClient _client;

    public WebClientFacade(WebClient client)
    {
      _client = client;
    }

    public string UploadString(Uri address, string data)
    {

#if NET45
      var holdingPen = ServicePointManager.SecurityProtocol;
      ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#elif NET40
      var holdingPen = ServicePointManager.SecurityProtocol;
      ServicePointManager.SecurityProtocol |= (SecurityProtocolType)768;//TLS 1.1
      ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; //TLS 1.2
#endif

      var result = _client.UploadString(address, data);

#if NET40 || NET45
      ServicePointManager.SecurityProtocol = holdingPen;
#endif
      return result;
    }

    public void Dispose()
    {
      _client?.Dispose();
    }
  }
}
