using NUnit.Framework;

namespace Raygun.Raygun4Net.Tests;

public class Tests
{

  [SetUp]
  public void Setup()
  {
  }

  [Test]
  public void Test1()
  {
    Class1 sut = new Class1();
    Assert.NotNull(sut);
  }
}