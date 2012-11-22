using System;
using System.Net;

using Newtonsoft.Json.Linq;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    public void Send(Exception exception)
    {
      Send(new RaygunMessage(exception));
    }

    public void Send(RaygunMessage raygunMessage)
    {
      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", RaygunSettings.Settings.ApiKey);

        client.UploadString(RaygunSettings.Settings.ApiEndpoint, JObject.FromObject(raygunMessage).ToString());
      }
    }
  }
}