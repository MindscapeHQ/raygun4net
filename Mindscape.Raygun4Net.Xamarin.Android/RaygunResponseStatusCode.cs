using System;
namespace Mindscape.Raygun4Net
{
  public enum RaygunResponseStatusCode
  {
    Accepted = 202,
    BadMessage = 400,
    InvalidApiKey = 403,
    LargePayload = 413,
    RateLimited = 429,
  }

  public class RaygunResponseStatusCodeConverter
  {
    public static string ToString(int statusCode)
    {
      switch (statusCode)
      {
        case (int)RaygunResponseStatusCode.Accepted: 
        return "Request succeeded";
        
        case (int)RaygunResponseStatusCode.BadMessage:
        return "Bad message - could not parse the provided JSON. Check all fields are present, especially both occurredOn (ISO 8601 DateTime) and details { } at the top level";
        
        case (int)RaygunResponseStatusCode.InvalidApiKey:
        return "Invalid API Key - The value specified in the header X-ApiKey did not match with an application in Raygun";
       
        case (int)RaygunResponseStatusCode.LargePayload:
        return "Request entity too large - The maximum size of a JSON payload is 128KB";
        
        case (int)RaygunResponseStatusCode.RateLimited: 
        return "Too Many Requests - Plan limit exceeded for month or plan expired";
       
        default: 
        return "Response status code: " + statusCode.ToString();
      }
    }
  }
}