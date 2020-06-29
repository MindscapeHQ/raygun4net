using System;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi
{
  internal interface IRaygunWebApiClientProvider
  {
    RaygunWebApiClient GenerateRaygunWebApiClient(HttpRequestMessage currentRequest = null);
  }
}