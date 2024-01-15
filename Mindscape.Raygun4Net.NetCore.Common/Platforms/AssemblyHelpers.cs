using System;
using System.Linq;
using System.Reflection;

namespace Mindscape.Raygun4Net.Platforms
{
  internal static class AssemblyHelpers
  {
    internal static Assembly FindAssembly(string name, byte[] publicKeyToken = null)
    {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();

      foreach (var assembly in assemblies)
      {
        var assemblyName = assembly.GetName();
        var publicKeyMatches = publicKeyToken == null ||
                               assemblyName.GetPublicKeyToken()?.SequenceEqual(publicKeyToken) is true;
        if (assemblyName.Name == name && publicKeyMatches)
        {
          return assembly;
        }
      }

      return null;
    }

    public static byte[] HexStringToByteArray(string hex)
    {
      var numberChars = hex.Length;
      var bytes = new byte[numberChars / 2];
      for (var i = 0; i < numberChars; i += 2)
      {
        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
      }

      return bytes;
    }
  }
}