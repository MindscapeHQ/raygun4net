using System;

namespace Mindscape.Raygun4Net.Utils
{
  public abstract class Singleton<T> where T : class
  {
    private static T _instance;
    private static object _lock = new object();

    protected Singleton() { }

    public static T Instance
    {
      get
      {
        lock (_lock)
        {
          if (_instance == null)
          {
            _instance = Activator.CreateInstance<T>();
          }

          return _instance;
        }
      }
    }
  }
}