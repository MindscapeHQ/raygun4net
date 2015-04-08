using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Messages;
using Microsoft.AspNet.Http;

namespace Mindscape.Raygun4Net.AspNet5.Builders
{
  public class RaygunAspNet5RequestMessageBuilder
  {
    public static async Task<RaygunRequestMessage> Build(HttpContext context, RaygunRequestMessageOptions options)
    {
      var request = context.Request;
      var message = new RaygunRequestMessage();

      options = options ?? new RaygunRequestMessageOptions();

      message.HostName = request.Host.Value;
      //message.Url = context.;
      message.HttpMethod = request.Method;
      message.IPAddress = GetIpAddress(request);
      message.Form = ToDictionary(await request.ReadFormAsync(), options.IsFormFieldIgnored);
      message.QueryString = ToDictionary(request.Query, f => false);

      if (!options.IsRawDataIgnored)
      {
        try
        {
          using (var reader = new StreamReader(request.Body))
          {
            message.RawData = reader.ReadToEnd();
          }
// ReSharper disable once EmptyGeneralCatchClause
        } catch {}
      }

      SetHeaders(message, request, options.IsHeaderIgnored);

      return message;
    }

    private static string GetIpAddress(HttpRequest request)
    {
      return "";
      //string ip = request.RemoteIpAddress ?? request.LocalIpAddress;
      //int? port = request.RemotePort ?? request.LocalPort;
      //if (string.IsNullOrWhiteSpace(ip)) return "";

      //if (port.HasValue) return ip + ":" + port.Value;
      
      //return ip;
    }

    private static void SetHeaders(RaygunRequestMessage message, HttpRequest request, Func<string, bool> ignored)
    {
      message.Headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        message.Headers[header.Key] = string.Join(",", header.Value);
      }
    }

    private static IDictionary ToDictionary(IEnumerable<KeyValuePair<string, string[]>> query, Func<string, bool> isFormFieldIgnored)
    {
      var dict = new Dictionary<string, string>();
      foreach(var value in query.Where(v => isFormFieldIgnored(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }
      return dict;
    }
  }
}