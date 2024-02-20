using FluentAssertions;
using Mindscape.Raygun4Net.AspNetCore.Builders;

namespace Mindscape.Raygun4Net.AspNetCore.Tests;

[TestFixture]
public class RaygunAspNetCoreRequestMessageBuilderTests
{
  // Any
  [TestCase("Banana", "*", true)]
  [TestCase("Apple", "*", true)]
  // Begins With
  [TestCase("Apple", "Ba*", false)]
  [TestCase("Banana", "Ba*", true)]
  // Ends With
  [TestCase("Apple", "*le", true)]
  [TestCase("Banana", "*le", false)]
  // Contains
  [TestCase("Apple", "*pl*", true)]
  [TestCase("Banana", "*pl*", false)]
  // Length Checks
  [TestCase("A", "**", false)]
  [TestCase("A", "*a*", true)]
  [TestCase("A", "*z*", false)]
  public void Banana(string key, string ignoredKey, bool expected)
  {
    RaygunAspNetCoreRequestMessageBuilder.IsIgnored(key, new[] { ignoredKey }).Should().Be(expected);
  }
}