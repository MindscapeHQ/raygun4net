using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharedProject1
{
  public class Class2
  {
    public String ImplementationType
    {
      get
      {
        var q = GetImplementation();
       return q.ToString(); }
    }


    public enum Implementation { Framework, Net, Core, Native, Xamarin }

    public Implementation GetImplementation()
    {
#if NET35 || NET40 || NET45
      return Implementation.Framework;
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
          case ".NET":
            return Implementation.Net;
          case ".NET Framework":
            return Implementation.Framework;
          case ".NET Core":
            return Implementation.Core;
          case ".NET Native":
            return Implementation.Native;
          default:
            throw new ArgumentException($"descr: {descr}");
        }
      }
#endif
    }
  }
}
