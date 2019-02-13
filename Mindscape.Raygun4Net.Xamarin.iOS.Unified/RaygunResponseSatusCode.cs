using System;
namespace Mindscape.Raygun4Net
{
  public enum RaygunResponseStatusCode
  {
    Accepted       = 202,
    BadMessage     = 400,
    InvalidApiKey  = 403,
    RequestTimeout = 408,
    LargePayload   = 413,
    RateLimited    = 429,
    GatewayTimeout = 504,
  }

  public class RaygunResponseStatusCodeConverter
  {
    public static string ToString(int statusCode)
    {
      switch (statusCode)
      {
        case (int)RaygunResponseStatusCode.Accepted:
          return "(202) Request succeeded";

        case (int)RaygunResponseStatusCode.BadMessage:
          return "(400) Bad message - Could not parse the provided JSON.";

        case (int)RaygunResponseStatusCode.InvalidApiKey:
          return "(403) Invalid API Key - The value specified in the header X-ApiKey did not match with an application in Raygun";

        case (int)RaygunResponseStatusCode.RequestTimeout:
          return "(408) Request timed out - Look to increase the time out value in the RaygunSettings";

        case (int)RaygunResponseStatusCode.LargePayload:
          return "(413) Request entity too large - The maximum size of a JSON payload is 128KB";

        case (int)RaygunResponseStatusCode.RateLimited:
          return "(429) Too Many Requests - Plan limit exceeded for month or plan expired";

        case (int)RaygunResponseStatusCode.GatewayTimeout:
          return "(504) Gateway Timeout";

        default:
          return "Response status code: " + statusCode.ToString();
      }
    }
  }
}
