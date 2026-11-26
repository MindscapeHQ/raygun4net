using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  [TestFixture]
  public class RaygunRequestMessageBuilderTests
  {
    private const string RequestBody = "{\"UserName\":\"Raygun\",\"Message\":\"Hello\"}";

    [Test]
    public void GetRawDataReturnsTheRequestBody()
    {
      var request = FakeHttpWorkerRequest.CreateRequest("POST", "application/json", Encoding.UTF8.GetBytes(RequestBody));

      var rawData = RaygunRequestMessageBuilder.GetRawData(request, new RaygunRequestMessageOptions());

      Assert.That(rawData, Is.EqualTo(RequestBody));
    }

    [Test]
    public void GetRawDataLeavesTheRequestInputStreamOpen()
    {
      // ASP.NET owns the request stream, and the application can still read it after Raygun has
      var request = FakeHttpWorkerRequest.CreateRequest("POST", "application/json", Encoding.UTF8.GetBytes(RequestBody));

      RaygunRequestMessageBuilder.GetRawData(request, new RaygunRequestMessageOptions());

      request.InputStream.Seek(0, SeekOrigin.Begin);
      using (var reader = new StreamReader(request.InputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
      {
        Assert.That(reader.ReadToEnd(), Is.EqualTo(RequestBody));
      }
    }

    [Test]
    public void RawDataRemainsUnchangedWhenParsingFails()
    {
      var rawData = "I am unchanged!";

      var options = new RaygunRequestMessageOptions();
      options.AddSensitiveFieldNames("password");

      Assert.That(rawData.Length, Is.EqualTo(15));

      var filteredData = RaygunRequestMessageBuilder.StripSensitiveValues(rawData, options);

      Assert.That(filteredData, Is.Not.Null);
      Assert.That(filteredData.Length, Is.EqualTo(15));
      Assert.That(filteredData, Is.EqualTo("I am unchanged!"));
    }

    [Test]
    public void DataContainsReturnsTrueWhenMatchingOnExactCase()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var containsSensitiveData = RaygunRequestMessageBuilder.DataContains(rawData, new List<string>() { "Password" });

      Assert.That(containsSensitiveData, Is.EqualTo(true));
    }

    [Test]
    public void DataContainsReturnsFalseWhenCaseDoesNotMatch()
    {
      var rawData = "{\"UserName\":\"Raygun\",\"Password\":\"123456\"}";

      var containsSensitiveData = RaygunRequestMessageBuilder.DataContains(rawData, new List<string>() { "password" });

      Assert.That(containsSensitiveData, Is.EqualTo(false));
    }
  }
}
