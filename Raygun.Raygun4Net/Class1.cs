using System;
using SharedProject1;

namespace Raygun.Raygun4Net
{
  public class Class1: Class2
  {
    


#if NET8_0
    public String Framework => "NET8";
#elif NET7_0
    public String Framework => "NET7";
#elif NET6_0
    public String Framework => "NET6";
#elif NET5_0
    public String Framework => "NET5";
#elif NET48
    public String Framework => "NET48";
#elif NET472
    public String Framework => "NET472";
#elif NET471
    public String Framework => "NET471";
#elif NET47
    public String Framework => "NET47";
#elif NET462
    public String Framework => "NET462";
#elif NET461
    public String Framework => "NET461";
#elif NET46
    public String Framework => "NET46";
#elif NET452

    public String Framework => "NET452";
#elif NET451

    public String Framework => "NET451";
#elif NET45

    public String Framework => "NET45";
#elif NET40
    public String Framework => "NET40";
#elif IS_CLIENT
    public String Framework => "IS_CLIENT";
#elif NET35
    public String Framework => "NET35";

#elif NETStandard
    public String Framework => "NETStandard";

#else
    public String Framework => "Empty";


#endif

  }
}
