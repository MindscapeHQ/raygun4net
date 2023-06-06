using System;
using System.Net;

namespace Mindscape.Raygun4Net.Common.DataAccess
{
  public class WebClientFacade : IHttpClient
  {
    private readonly WebClient _client;

    public WebClientFacade(WebClient client)
    {
      _client = client;
    }

    public string UploadString(Uri address, string data)
    {
      var result = _client.UploadString(address, data);
      return result;
    }
  }
}
