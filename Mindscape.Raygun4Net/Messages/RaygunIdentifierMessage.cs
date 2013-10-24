using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunIdentifierMessage
  {
    public string Identifier { get; private set; }

    public RaygunIdentifierMessage(string user)
    {
      Identifier = user;
    }    
  }
}
