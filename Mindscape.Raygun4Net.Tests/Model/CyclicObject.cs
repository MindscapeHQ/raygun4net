using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net.Tests.Model
{
  public class CyclicObject
  {
    private CyclicObject _child;
    private readonly CyclicObject[] _array = new CyclicObject[1];
    private readonly IDictionary _dictionary = new Dictionary<object, object>();
    private readonly IDictionary<string, object> _genericDictionary = new Dictionary<string, object>();

    public CyclicObject Child
    {
      get { return _child; }
      set
      {
        _child = value;
      }
    }

    public CyclicObject[] Array
    {
      get { return _array; }
    }

    public IDictionary Dictionary
    {
      get { return _dictionary; }
    }

    public IDictionary<string, object> GenericDictionary
    {
      get { return _genericDictionary; }
    }
  }
}
