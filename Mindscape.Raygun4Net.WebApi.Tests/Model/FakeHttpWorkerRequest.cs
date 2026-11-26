using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace Mindscape.Raygun4Net.WebApi.Tests.Model
{
  /// <summary>
  /// Builds a real ASP.NET <see cref="HttpRequest"/> with a request body, so that
  /// <see cref="HttpRequest.InputStream"/> is the stream ASP.NET gives the application.
  /// </summary>
  public class FakeHttpWorkerRequest : SimpleWorkerRequest
  {
    private readonly string _httpMethod;
    private readonly string _contentType;
    private readonly byte[] _body;

    public FakeHttpWorkerRequest(string httpMethod, string contentType, byte[] body)
      : base("/", AppDomain.CurrentDomain.BaseDirectory, "default.aspx", string.Empty, new StringWriter())
    {
      _httpMethod = httpMethod;
      _contentType = contentType;
      _body = body;
    }

    public static HttpRequest CreateRequest(string httpMethod, string contentType, byte[] body)
    {
      return new HttpContext(new FakeHttpWorkerRequest(httpMethod, contentType, body)).Request;
    }

    public override string GetHttpVerbName()
    {
      return _httpMethod;
    }

    public override string GetKnownRequestHeader(int index)
    {
      switch (index)
      {
        case HeaderContentType:
          return _contentType;
        case HeaderContentLength:
          return _body.Length.ToString(CultureInfo.InvariantCulture);
        default:
          return base.GetKnownRequestHeader(index);
      }
    }

    public override byte[] GetPreloadedEntityBody()
    {
      return _body;
    }

    public override int GetPreloadedEntityBodyLength()
    {
      return _body.Length;
    }

    public override int GetTotalEntityBodyLength()
    {
      return _body.Length;
    }

    public override bool IsEntireEntityBodyIsPreloaded()
    {
      return true;
    }
  }
}
