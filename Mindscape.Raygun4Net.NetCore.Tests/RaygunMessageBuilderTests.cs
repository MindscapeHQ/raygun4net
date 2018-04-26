using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class RaygunMessageBuilderTests
  {
    private RaygunSettings _settings;
    private RaygunMessageBuilder _builder;

    [SetUp]
    public void SetUp()
    {
      _settings = new RaygunSettings();
      _builder = RaygunMessageBuilder.New(_settings);
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
