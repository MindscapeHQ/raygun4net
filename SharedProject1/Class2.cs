using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharedProject1
{
  public class Class2
  {
    public String sdkjfhgaskj => GetImplementation().ToString();


    public enum Implementation { Classic, Core, Native, Xamarin }

    public Implementation GetImplementation()
    {
#if NET40 || NET45
      return Implementation.Classic;
#else


      if (Type.GetType("Xamarin.Forms.Device") != null)
      {
        return Implementation.Xamarin;
      }
      else
      {
        var descr = RuntimeInformation.FrameworkDescription;
        var platf = descr.Substring(0, descr.LastIndexOf(' '));
        switch (platf)
        {
          case ".NET Framework":
            return Implementation.Classic;
          case ".NET Core":
            return Implementation.Core;
          case ".NET Native":
            return Implementation.Native;
          default:
            throw new ArgumentException();
        }
      }
#endif
    }
  }
}
