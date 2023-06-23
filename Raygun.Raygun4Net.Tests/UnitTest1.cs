using System;
using NUnit.Framework;
using SharedProject1;

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

#if NET48_OR_GREATER

    Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
#endif

    Console.WriteLine(sut.Framework);
    Console.WriteLine(sut.ImplementationType);


    Assert.NotNull(sut);
  }
}