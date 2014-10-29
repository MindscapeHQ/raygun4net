using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class SimpleJsonTests
  {
    [Test]
    public void SerializeNonStringDictionary()
    {
      Dictionary<int, string> data = new Dictionary<int, string>();
      data[0] = "First!";

      RaygunMessage message = new RaygunMessage
      {
        OccurredOn = new DateTime(),
        Details = new RaygunMessageDetails
        {
          Error = new RaygunErrorMessage
          {
            Data = data
          }
        }
      };

      string messageString = SimpleJson.SerializeObject(message);

      Assert.AreEqual("{\"OccurredOn\":\"0001-01-01T00:00:00Z\",\"Details\":{\"Error\":{\"Data\":{\"0\":\"First!\"}}}}", messageString);
    }
  }
}
