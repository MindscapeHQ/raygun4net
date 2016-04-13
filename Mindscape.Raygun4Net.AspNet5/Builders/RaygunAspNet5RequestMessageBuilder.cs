using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Messages;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Extensions;

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
      message.Url = request.GetDisplayUrl();
      message.HttpMethod = request.Method;
      message.IPAddress = GetIpAddress(context.Connection);
      try
      {
        if(request.HasFormContentType)
        {
          message.Form = ToDictionary(await request.ReadFormAsync(), options.IsFormFieldIgnored);
        }
      }
// ReSharper disable once EmptyGeneralCatchClause
      catch {}

      try
      {
        message.QueryString = ToDictionary(request.Query, f => false);
      }
// ReSharper disable once EmptyGeneralCatchClause
      catch { }

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

    private static string GetIpAddress(ConnectionInfo request)
    {
      var ip = request.RemoteIpAddress ?? request.LocalIpAddress;
      if (ip == null) return "";
      int? port = request.RemotePort == 0 ? request.LocalPort : request.RemotePort;

      if (port != 0) return ip + ":" + port.Value;

      return ip.ToString();
    }

    private static void SetHeaders(RaygunRequestMessage message, HttpRequest request, Func<string, bool> ignored)
    {
      message.Headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        message.Headers[header.Key] = string.Join(",", header.Value);
      }
    }

    private static IDictionary ToDictionary(IReadableStringCollection query, Func<string, bool> isFormFieldIgnored)
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