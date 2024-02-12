using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
    public class CyclicObject
    {
        private CyclicObject _child;
        private CyclicObject[] _array = new CyclicObject[1];
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
            set
            {
                _array = value;
            }
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
