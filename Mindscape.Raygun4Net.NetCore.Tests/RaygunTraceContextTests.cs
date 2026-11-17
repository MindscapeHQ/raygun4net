#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  [NonParallelizable]
  public class RaygunTraceContextTests
  {
    private const string TraceId = "0123456789abcdef0123456789abcdef";
    private const string ParentSpanId = "0123456789abcdef";
    private Activity _previousActivity;

    [SetUp]
    public void SetUp()
    {
      _previousActivity = Activity.Current;
      Activity.Current = null;
    }

    [TearDown]
    public void TearDown()
    {
      Activity.Current = _previousActivity;
    }

    [Test]
    public void NoActivity_OmitsTraceContextWithoutCreatingAnActivity()
    {
      var message = Build();

      Assert.Multiple(() =>
      {
        Assert.That(message.Details.TraceContext, Is.Null);
        Assert.That(Activity.Current, Is.Null);
        Assert.That(SimpleJson.SerializeObject(message), Does.Not.Contain("TraceContext"));
      });
    }

    [Test]
    public void HierarchicalActivity_OmitsTraceContext()
    {
      using var activity = new Activity("hierarchical").SetIdFormat(ActivityIdFormat.Hierarchical).Start();

      var message = Build();

      Assert.Multiple(() =>
      {
        Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.Hierarchical));
        Assert.That(message.Details.TraceContext, Is.Null);
        Assert.That(SimpleJson.SerializeObject(message), Does.Not.Contain("TraceContext"));
      });
    }

    [TestCase("01", true)]
    [TestCase("00", false)]
    public void W3cActivity_CapturesContextRegardlessOfSampling(string flags, bool recorded)
    {
      using var activity = new Activity("request")
        .SetIdFormat(ActivityIdFormat.W3C)
        .SetParentId($"00-{TraceId}-{ParentSpanId}-{flags}")
        .Start();
      activity.IsAllDataRequested = recorded;

      var message = Build();

      Assert.Multiple(() =>
      {
        Assert.That(activity.Recorded, Is.EqualTo(recorded));
        Assert.That(message.Details.TraceContext.TraceId, Is.EqualTo(TraceId));
        Assert.That(message.Details.TraceContext.SpanId, Is.EqualTo(activity.SpanId.ToHexString()));
        Assert.That(message.Details.TraceContext.TraceId, Does.Match("^[0-9a-f]{32}$"));
        Assert.That(message.Details.TraceContext.SpanId, Does.Match("^[0-9a-f]{16}$"));
        Assert.That(SimpleJson.SerializeObject(message.Details.TraceContext),
          Is.EqualTo($"{{\"TraceId\":\"{TraceId}\",\"SpanId\":\"{activity.SpanId.ToHexString()}\"}}"));
        Assert.That(SimpleJson.SerializeObject(message), Does.Contain("\"TraceContext\":{"));
      });
    }

    [Test]
    public void ActivityStoppedBeforeSerialization_KeepsTheCapturedIdentifiers()
    {
      var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
      var expectedTraceId = activity.TraceId.ToHexString();
      var expectedSpanId = activity.SpanId.ToHexString();
      var message = Build();
      activity.Dispose();

      Assert.Multiple(() =>
      {
        Assert.That(Activity.Current, Is.Null);
        Assert.That(message.Details.TraceContext.TraceId, Is.EqualTo(expectedTraceId));
        Assert.That(message.Details.TraceContext.SpanId, Is.EqualTo(expectedSpanId));
      });
    }

    [Test]
    public void ReusedBuilderWithoutAnActivity_DoesNotRetainPreviousContext()
    {
      var builder = RaygunMessageBuilder.New(new RaygunSettings());
      using (new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start())
      {
        Assert.That(builder.SetExceptionDetails(new Exception()).Build().Details.TraceContext, Is.Not.Null);
      }

      var message = builder.SetExceptionDetails(new Exception()).Build();

      Assert.That(message.Details.TraceContext, Is.Null);
    }

    [Test]
    public void UnstartedW3cActivity_DoesNotEmitEmptyIdentifiers()
    {
      using var activity = new Activity("unstarted").SetIdFormat(ActivityIdFormat.W3C);
      Activity.Current = activity;

      Assert.That(Build().Details.TraceContext, Is.Null);
    }

    private static RaygunMessage Build() => RaygunMessageBuilder.New(new RaygunSettings())
      .SetExceptionDetails(new InvalidOperationException("fixture"))
      .Build();
  }
}
#endif
