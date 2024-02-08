using System;
using System.Net;
using System.Web;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunMessageBuilderTests
  {
    private RaygunMessageBuilder _builder;

    [SetUp]
    public void SetUp()
    {
      _builder = RaygunMessageBuilder.New;
    }
    
    [Test]
    public void New()
    {
      Assert.That(_builder, Is.Not.Null);
    }

    [Test]
    public void SetVersion()
    {
      IRaygunMessageBuilder builder = _builder.SetVersion("Custom Version");
      Assert.That(_builder, Is.EqualTo(builder));

      RaygunMessage message = _builder.Build();
      Assert.That("Custom Version", Is.EqualTo(message.Details.Version));
    }

    [Test]
    public void SetVersion_Null()
    {
#if DEBUG
      RaygunSettings.Settings = new RaygunSettings();//Mindscape.Raygun4Net.RaygunHttpModule is modifying this global object. So this is resetting it here.
#endif
      _builder.SetVersion(null);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Version, Is.EqualTo("1.0.0.0"));
    }

    [Test]
    public void OccurredOnIsNow()
    {
      RaygunMessage message = _builder.Build();
      Assert.That((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1, Is.True);
    }

    [Test]
    public void SetTimeStamp()
    {
      DateTime time = new DateTime(2015, 2, 16);
      RaygunMessage message = _builder.SetTimeStamp(time).Build();
      Assert.That(time, Is.EqualTo(message.OccurredOn));
    }

    [Test]
    public void SetNullTimeStamp()
    {
      RaygunMessage message = _builder.SetTimeStamp(null).Build();
      Assert.That((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1, Is.True);
    }

    // Response tests

    [Test]
    public void ResponseIsNullForNonWebExceptions()
    {
      NullReferenceException exception = new NullReferenceException("The thing is null");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Response, Is.Null);
    }

    [Test]
    public void GetStatusCodeFromHttpException()
    {
      HttpException exception = new HttpException(404, "the file is gone");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Response, Is.Not.Null);
      Assert.That(404, Is.EqualTo(message.Details.Response.StatusCode));
      Assert.That("NotFound", Is.EqualTo(message.Details.Response.StatusDescription));
    }

    [Test]
    public void HandleUnknownStatusCodeFromHttpException()
    {
      HttpException exception = new HttpException(1, "?");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Response, Is.Not.Null);
      Assert.That(1, Is.EqualTo(message.Details.Response.StatusCode));
      Assert.That(message.Details.Response.StatusDescription, Is.Null);
    }

    [Test]
    public void GetStatusCodeFromWebException()
    {
      Exception exception = null;
      try
      {
        WebRequest request = WebRequest.Create("http://www.google.com/missing.html");
        request.GetResponse();
      }
      catch (Exception e)
      {
        exception = e;
      }
      Assert.That(exception, Is.Not.Null);

      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Response, Is.Not.Null);
      Assert.That(404, Is.EqualTo(message.Details.Response.StatusCode));
      Assert.That("Not Found", Is.EqualTo(message.Details.Response.StatusDescription));
    }

    [Test]
    public void GetStatusDescriptionFromWebException_NonProtocolError()
    {
      Exception exception = null;
      try
      {
        WebRequest request = WebRequest.Create("http://missing.html");
        request.GetResponse();
      }
      catch (Exception e)
      {
        exception = e;
      }
      Assert.That(exception, Is.Not.Null);

      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.That(message.Details.Response, Is.Not.Null);
      Assert.That(0, Is.EqualTo(message.Details.Response.StatusCode));
      Assert.That("NameResolutionFailure", Is.EqualTo(message.Details.Response.StatusDescription));
    }
  }
}
