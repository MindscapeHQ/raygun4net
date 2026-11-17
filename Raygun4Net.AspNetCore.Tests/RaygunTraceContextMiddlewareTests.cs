using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

[TestFixture]
[NonParallelizable]
public class RaygunTraceContextMiddlewareTests
{
  [Test]
  public void ExceptionAfterChildActivityIsDisposed_CapturesRequestContextBeforeAsyncMessageBuilder()
  {
    using var requestActivity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
    var context = new DefaultHttpContext();
    var accessor = new HttpContextAccessor { HttpContext = context };
    var settings = new RaygunSettings
    {
      ApiKey = "fixture",
      IsRawDataIgnored = true,
      IsDiskSpaceFreeIgnored = true,
      ExcludeErrorsFromLocal = false
    };
    var builder = new AsyncMessageBuilder();
    var client = new RaygunClient(settings, null!, new[] { builder });
    client.SendingMessage += (_, args) => args.Cancel = true;
    var middleware = new RaygunMiddleware(async _ =>
    {
      using var child = new Activity("controller-child").Start();
      await Task.Yield();
      throw new InvalidOperationException("fixture");
    }, settings, client, accessor);

    Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));

    Assert.Multiple(() =>
    {
      Assert.That(builder.ContextBeforeAwait, Is.Not.Null);
      Assert.That(builder.ContextBeforeAwait!.TraceId, Is.EqualTo(requestActivity.TraceId.ToHexString()));
      Assert.That(builder.ContextBeforeAwait.SpanId, Is.EqualTo(requestActivity.SpanId.ToHexString()));
      Assert.That(builder.ContextAfterAwait, Is.SameAs(builder.ContextBeforeAwait));
      Assert.That(builder.ActivityDuringBuilder, Is.Not.Null);
      Assert.That(builder.ActivityDuringBuilder, Is.Not.SameAs(requestActivity));
      Assert.That(builder.ActivityDuringBuilder!.TraceId, Is.EqualTo(requestActivity.TraceId));
      Assert.That(builder.ActivityDuringBuilder.ParentSpanId, Is.EqualTo(requestActivity.SpanId));
      Assert.That(Activity.Current, Is.SameAs(requestActivity));
    });
  }

  private sealed class AsyncMessageBuilder : IMessageBuilder
  {
    public RaygunTraceContext? ContextBeforeAwait { get; private set; }
    public RaygunTraceContext? ContextAfterAwait { get; private set; }
    public Activity? ActivityDuringBuilder { get; private set; }

    public async Task<RaygunMessage> Apply(RaygunMessage message, Exception exception)
    {
      ContextBeforeAwait = message.Details.TraceContext;
      using var child = new Activity("message-builder-child").Start();
      await Task.Yield();
      ActivityDuringBuilder = Activity.Current;
      ContextAfterAwait = message.Details.TraceContext;
      return message;
    }
  }
}
