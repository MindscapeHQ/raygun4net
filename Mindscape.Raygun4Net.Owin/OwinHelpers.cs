using OwinEnvironment = System.Collections.Generic.IDictionary<string, object>;

namespace Mindscape.Raygun4Net.Owin
{
  public static class OwinHelpers
  {
    public static T Get<T>(this OwinEnvironment env, string key)
    {
      object value;
      if (env.TryGetValue(key, out value))
      {
        return (T)value;
      }

      return default(T);
    }
  }
}