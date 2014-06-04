using System;
using System.Collections.Generic;
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
      _builder.SetVersion(null);
      RaygunMessage message = _builder.Build();
      Assert.AreEqual("Not supplied", message.Details.Version);
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
