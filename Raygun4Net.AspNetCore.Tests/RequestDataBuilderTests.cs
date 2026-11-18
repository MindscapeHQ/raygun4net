using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore.Builders;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

[TestFixture]
public class RequestDataBuilderTests
{
  [Test]
  public async Task Apply_AddsRequestAndResponse_WhenThereIsAnHttpContext()
  {
    var context = new DefaultHttpContext();
    context.Request.Scheme = "https";
    context.Request.Host = new HostString("example.com");
    context.Request.Path = "/orders";
    context.Response.StatusCode = 500;

    var message = await CreateBuilder(context).Apply(new RaygunMessage(), new Exception("test"));

    message.Details.Request.Url.Should().Be("https://example.com/orders");
    message.Details.Response.StatusCode.Should().Be(500);
  }

  [Test]
  public async Task Apply_LeavesRequestAndResponseUnset_WhenThereIsNoHttpContext()
  {
    var message = await CreateBuilder(null).Apply(new RaygunMessage(), new Exception("test"));

    message.Details.Request.Should().BeNull();
    message.Details.Response.Should().BeNull();
  }

  [Test]
  public async Task Apply_KeepsRequestAndResponse_WhenTheyAreAlreadySet()
  {
    var request = new RaygunRequestMessage { Url = "https://example.com/original" };
    var response = new RaygunResponseMessage { StatusCode = 404 };
    var message = new RaygunMessage { Details = { Request = request, Response = response } };

    await CreateBuilder(new DefaultHttpContext()).Apply(message, new Exception("test"));

    message.Details.Request.Should().BeSameAs(request);
    message.Details.Response.Should().BeSameAs(response);
  }

  private static RequestDataBuilder CreateBuilder(HttpContext? context)
  {
    return new RequestDataBuilder(new FixedHttpContextAccessor { HttpContext = context }, new RaygunSettings());
  }

  private class FixedHttpContextAccessor : IHttpContextAccessor
  {
    public HttpContext? HttpContext { get; set; }
  }
}
