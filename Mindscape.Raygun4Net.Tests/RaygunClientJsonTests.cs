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
    private RaygunClient raygunClient;    

    [SetUp]
    public void SetUp()
    {
      raygunClient = new RaygunClient();       
    }
  }
}