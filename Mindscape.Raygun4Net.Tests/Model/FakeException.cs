using System;
using System.Collections;

namespace Mindscape.Raygun4Net.Tests
{
  public class FakeException : Exception
  {
    private IDictionary _data;

    public FakeException(IDictionary data)
    {
      _data = data;
    }

    public override System.Collections.IDictionary Data
    {
      get
      {
        return _data;
      }
    }
  }
}
