using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class SimpleJsonTests
  {
    private CyclicObject _cyclicObject;
    private CyclicObject _cyclicArray;
    private CyclicObject _cyclicDictionary;
    private CyclicObject _cyclicGenericDictionary;
    private CyclicObject _deepCyclicObject;
    private CyclicObject _siblingObject;
#if  DEBUG
    private string token = "null";
#else
    private string token = "d595e487e4f9f950";
#endif

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

      _deepCyclicObject = new CyclicObject();
      _deepCyclicObject.Child = new CyclicObject();
      _deepCyclicObject.Child.Array[0] = new CyclicObject();
      _deepCyclicObject.Child.Array[0].Dictionary["Key"] = _deepCyclicObject;

      _siblingObject = new CyclicObject();
      _siblingObject.Child = new CyclicObject();
      _siblingObject.Array[0] = _siblingObject.Child;
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

      Assert.That("{\"OccurredOn\":\"0001-01-01T00:00:00Z\",\"Details\":{\"Error\":{\"Data\":{\"0\":\"First!\"}}}}", Is.EqualTo(messageString));
    }

    // Array tests

    [Test]
    public void ArrayContainingNull()
    {
      CyclicObject co = new CyclicObject();
      co.Array = new CyclicObject[3];
      string json = SimpleJson.SerializeObject(co);
      Assert.That("{\"Array\":[null,null,null],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    // Dictionary tests

    [Test]
    public void NullInDictionaryDoesNotSerialize()
    {
      CyclicObject co = new CyclicObject();
      co.Dictionary["Key"] = null;
      string json = SimpleJson.SerializeObject(co);
      Assert.That("{\"Array\":[null],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    // Type tests

    [Test]
    public void SerializeTypeObject()
    {
      string json = SimpleJson.SerializeObject(typeof(FakeRaygunClient));
      Assert.That(@$"""Mindscape.Raygun4Net.NetCore.Tests.FakeRaygunClient, Mindscape.Raygun4Net.NetCore.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken={token}""", Is.EqualTo(json));
    }

    [Test]
    public void SerializeTypeProperty()
    {
      var o = new { Type = typeof(FakeRaygunClient) };
      string json = SimpleJson.SerializeObject(o);
      Assert.That(@$"{{""Type"":""Mindscape.Raygun4Net.NetCore.Tests.FakeRaygunClient, Mindscape.Raygun4Net.NetCore.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken={token}""}}", Is.EqualTo(json));
    }

    // Cyclic object structure tests

    [Test]
    public void HandleCircularObjectStructure()
    {
      string json = SimpleJson.SerializeObject(_cyclicObject);
      Assert.That("{\"Child\":\"" + SimpleJson.CYCLIC_MESSAGE + "\",\"Array\":[null],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    [Test]
    public void HandleCircularObjectStructureWithinArray()
    {
      string json = SimpleJson.SerializeObject(_cyclicArray);
      Assert.That("{\"Array\":[\"" + SimpleJson.CYCLIC_MESSAGE + "\"],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    [Test]
    public void HandleCircularObjectStructureWithinDictionary()
    {
      string json = SimpleJson.SerializeObject(_cyclicDictionary);
      Assert.That("{\"Array\":[null],\"Dictionary\":{\"Key\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    [Test]
    public void HandleCircularObjectStructureWithinGenericDictionary()
    {
      string json = SimpleJson.SerializeObject(_cyclicGenericDictionary);
      Assert.That("{\"Array\":[null],\"Dictionary\":{},\"GenericDictionary\":{\"Key\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"}}", Is.EqualTo(json));
    }

    [Test]
    public void HandleDeepCircularObjectStructure()
    {
      string json = SimpleJson.SerializeObject(_deepCyclicObject);
      Assert.That("{\"Child\":{\"Array\":[{\"Array\":[null],\"Dictionary\":{\"Key\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"},\"GenericDictionary\":{}}],\"Dictionary\":{},\"GenericDictionary\":{}},\"Array\":[null],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    [Test]
    public void SerizlizeTheSameObjectWhenNotInACircularObjectStructure()
    {
      string json = SimpleJson.SerializeObject(_siblingObject);
      // The same object is repeated twice within the same parent, but there are siblings and don't form a cycle, so the json does not mention the cyclic message.
      // This is to test that objects are removed from the visited stack as the serializer traverses back through the object structure.
      Assert.That("{\"Child\":{\"Array\":[null],\"Dictionary\":{},\"GenericDictionary\":{}},\"Array\":[{\"Array\":[null],\"Dictionary\":{},\"GenericDictionary\":{}}],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    [Test]
    public void HandleCircularObjectStructureInMultipleBranches_Array()
    {
      CyclicObject cyclicObject = new CyclicObject();
      cyclicObject.Array = new CyclicObject[4];
      cyclicObject.Array[0] = cyclicObject;
      cyclicObject.Array[1] = cyclicObject;
      cyclicObject.Array[2] = cyclicObject;
      cyclicObject.Array[3] = cyclicObject;

      string json = SimpleJson.SerializeObject(cyclicObject);
      Assert.That("{\"Array\":[\"" + SimpleJson.CYCLIC_MESSAGE + "\",\"" + SimpleJson.CYCLIC_MESSAGE + "\",\"" + SimpleJson.CYCLIC_MESSAGE + "\",\"" + SimpleJson.CYCLIC_MESSAGE + "\"],\"Dictionary\":{},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }

    [Test]
    public void HandleCircularObjectStructureInMultipleBranches_Dictionary()
    {
      CyclicObject cyclicObject = new CyclicObject();
      cyclicObject.Dictionary["Key1"] = cyclicObject;
      cyclicObject.Dictionary["Key2"] = cyclicObject;
      cyclicObject.Dictionary["Key3"] = cyclicObject;
      cyclicObject.Dictionary["Key4"] = cyclicObject;

      string json = SimpleJson.SerializeObject(cyclicObject);
      Assert.That("{\"Array\":[null],\"Dictionary\":{\"Key1\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"," + "\"Key2\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"," + "\"Key3\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"," + "\"Key4\":\"" + SimpleJson.CYCLIC_MESSAGE + "\"},\"GenericDictionary\":{}}", Is.EqualTo(json));
    }
  }
}
