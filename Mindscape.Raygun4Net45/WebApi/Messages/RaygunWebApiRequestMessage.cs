using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.WebApi.Messages
{
  public class RaygunWebApiRequestMessage : IRaygunRequestMessage
  {
    public string HostName { get; set; }

    public string Url { get; set; }

    public string HttpMethod { get; set; }

    public string IPAddress { get; set; }

    public IDictionary QueryString { get; set; }

    public IList Cookies { get; set; }

    public IDictionary Data { get; set; }

    public IDictionary Form { get; set; }

    public string RawData { get; set; }

    public IDictionary Headers { get; set; }
  }
}