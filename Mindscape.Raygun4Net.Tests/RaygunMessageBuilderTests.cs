using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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
      Assert.IsNotNull(_builder);
    }

    [Test]
    public void SetVersion()
    {
      IRaygunMessageBuilder builder = _builder.SetVersion("Custom Version");
      Assert.AreEqual(_builder, builder);

      RaygunMessage message = _builder.Build();
      Assert.AreEqual("Custom Version", message.Details.Version);
    }

    [Test]
    public void SetVersion_Null()
    {
#if DEBUG
      RaygunSettings.Settings = new RaygunSettings();//Mindscape.Raygun4Net.RaygunHttpModule is modifying this global object. So this is resetting it here.
#endif
      _builder.SetVersion(null);
      RaygunMessage message = _builder.Build();
      Assert.AreEqual("Not supplied", message.Details.Version);
    }

    [Test]
    public void OccurredOnIsNow()
    {
      RaygunMessage message = _builder.Build();
      Assert.IsTrue((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1);
    }

    [Test]
    public void SetTimeStamp()
    {
      DateTime time = new DateTime(2015, 2, 16);
      RaygunMessage message = _builder.SetTimeStamp(time).Build();
      Assert.AreEqual(time, message.OccurredOn);
    }

    [Test]
    public void SetNullTimeStamp()
    {
      RaygunMessage message = _builder.SetTimeStamp(null).Build();
      Assert.IsTrue((DateTime.UtcNow - message.OccurredOn).TotalSeconds < 1);
    }

    // Response tests

    [Test]
    public void ResponseIsNullForNonWebExceptions()
    {
      NullReferenceException exception = new NullReferenceException("The thing is null");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.IsNull(message.Details.Response);
    }

    [Test]
    public void GetStatusCodeFromHttpException()
    {
      HttpException exception = new HttpException(404, "the file is gone");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.IsNotNull(message.Details.Response);
      Assert.AreEqual(404, message.Details.Response.StatusCode);
      Assert.AreEqual("NotFound", message.Details.Response.StatusDescription);
    }

    [Test]
    public void HandleUnknownStatusCodeFromHttpException()
    {
      HttpException exception = new HttpException(1, "?");
      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.IsNotNull(message.Details.Response);
      Assert.AreEqual(1, message.Details.Response.StatusCode);
      Assert.IsNull(message.Details.Response.StatusDescription);
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
      Assert.IsNotNull(exception);

      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.IsNotNull(message.Details.Response);
      Assert.AreEqual(404, message.Details.Response.StatusCode);
      Assert.AreEqual("Not Found", message.Details.Response.StatusDescription);
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
      Assert.IsNotNull(exception);

      _builder.SetExceptionDetails(exception);
      RaygunMessage message = _builder.Build();
      Assert.IsNotNull(message.Details.Response);
      Assert.AreEqual(0, message.Details.Response.StatusCode);
      Assert.AreEqual("NameResolutionFailure", message.Details.Response.StatusDescription);
    }
  }
}
