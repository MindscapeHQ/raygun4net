using System;
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

    Console.WriteLine(sut.Framework);
    Console.WriteLine(sut.sdkjfhgaskj);

    Assert.NotNull(sut);
  }
}