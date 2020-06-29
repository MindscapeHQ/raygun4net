using System;
using System.Net;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiHttpException : Exception
  {
    public RaygunWebApiHttpException(HttpStatusCode statusCode, string reasonPhrase, string message)
      : base(message)
    {
      ReasonPhrase = reasonPhrase;
      StatusCode = statusCode;
    }

    public RaygunWebApiHttpException(string message, HttpResponseMessage response) :
      base(message)
    {
      ReasonPhrase = response.ReasonPhrase;
      StatusCode = response.StatusCode;
      Content = RaygunSettings.Settings.IsResponseContentIgnored ? null : response.Content.ReadAsString();
    }

    public HttpStatusCode StatusCode { get; set; }

    public string ReasonPhrase { get; set; }

    public string Content { get; set; }
  }
}