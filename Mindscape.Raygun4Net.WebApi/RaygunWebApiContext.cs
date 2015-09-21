using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiContext
  {
    /// <summary>
    /// The current HttpRequest. May be null if there is no HttpRequest in the context of the error.
    /// </summary>
    public HttpRequestMessage RequestMessage { get; set; }
    /// <summary>
    /// The current HttpResponse. May be null if there is no HttpResponse in the context of the error,
    /// the error caused no response to be sent, or if the error handling kicked in before the response
    /// was generated
    /// </summary>
    public HttpResponseMessage ResponseMessage { get; set; }

    public RaygunWebApiContext(HttpRequestMessage requestMessage)
    {
      RequestMessage = requestMessage;
    }

    public RaygunWebApiContext(HttpRequestMessage requestMessage, HttpResponseMessage responseMessage)
    {
      RequestMessage = requestMessage;
      ResponseMessage = responseMessage;
    }
  }
}