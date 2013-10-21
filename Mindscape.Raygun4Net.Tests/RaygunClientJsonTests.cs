using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunClientJsonTests
  {

    [Test]
    public void MessageWithUser()
    {
      var msg = RaygunMessageBuilder.New
                          .SetUser("test")
                          .Build();

      Equals("test", msg.Details.User);
    }
  }
}