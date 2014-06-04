using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunSettingsTests
  {
    [Test]
    public void Apikey_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.ApiKey);
    }

    [Test]
    public void ApiEndPoint_DefaultValue()
    {
      Assert.AreEqual("https://api.raygun.io/entries", RaygunSettings.Settings.ApiEndpoint.AbsoluteUri);
    }

    [Test]
    public void MediumTrust_FalseByDefault()
    {
      Assert.IsFalse(RaygunSettings.Settings.MediumTrust);
    }

    [Test]
    public void ThrowOnError_FalseByDefault()
    {
      Assert.IsFalse(RaygunSettings.Settings.ThrowOnError);
    }

    [Test]
    public void ExcludeErrorsFromLocal_FalseByDefault()
    {
      Assert.IsFalse(RaygunSettings.Settings.ExcludeErrorsFromLocal);
    }

    [Test]
    public void IgnoreFormDataNames_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.IgnoreFormDataNames);
    }
  }
}
