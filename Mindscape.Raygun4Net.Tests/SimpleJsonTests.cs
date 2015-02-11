using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Tests.Model;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class SimpleJsonTests
  {
    private CyclicObject _cyclicObject;
    private CyclicObject _cyclicArray;
    private CyclicObject _cyclicDictionary;
    private CyclicObject _cyclicGenericDictionary;

    [SetUp]
    public void SetUp()
    {
      _cyclicObject = new CyclicObject();
      _cyclicObject.Child = _cyclicObject;

      _cyclicArray = new CyclicObject();
      _cyclicArray.Array[0] = _cyclicArray;

      _cyclicDictionary = new CyclicObject();
      _cyclicDictionary.Dictionary["Key"] = _cyclicDictionary;

      _cyclicGenericDictionary = new CyclicObject();
      _cyclicGenericDictionary.GenericDictionary["Key"] = _cyclicGenericDictionary;
    }

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

    [Test]
    public void HandleCircularObjectStructure()
    {
      string json = SimpleJson.SerializeObject(_cyclicObject);
      Assert.IsTrue(json.Contains("\"Child\":\"" + SimpleJson.CYCLIC_MESSAGE + "\""));
    }

    [Test]
    public void HandleCircularObjectStructureWithinArray()
    {
      string json = SimpleJson.SerializeObject(_cyclicArray);
      Assert.IsTrue(json.Contains("\"Array\":[\"" + SimpleJson.CYCLIC_MESSAGE + "\"]"));
    }

    [Test]
    public void HandleCircularObjectStructureWithinDictionary()
    {
      string json = SimpleJson.SerializeObject(_cyclicDictionary);
      Assert.IsTrue(json.Contains("\"Dictionary\":{\"Key\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"}"));
    }

    [Test]
    public void HandleCircularObjectStructureWithinGenericDictionary()
    {

    }

    [Test]
    public void HandleDeepCircularObjectStructure()
    {

    }

    [Test]
    public void SerizlizeTheSameObjectWhenNotInACircularObjectStructure()
    {

    }
  }
}
