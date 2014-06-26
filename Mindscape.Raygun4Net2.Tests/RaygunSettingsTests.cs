using System;
using System.Collections.Generic;
using System.Text;
using Mindscape.Raygun4Net;
using NUnit.Framework;

namespace Mindscape.Raygun4Net2.Tests
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
    public void ExcludeHttpStatusCodesList_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.ExcludeHttpStatusCodesList);
    }

    [Test]
    public void ExcludeErrorsFromLocal_FalseByDefault()
    {
      Assert.IsFalse(RaygunSettings.Settings.ExcludeErrorsFromLocal);
    }

    [Test]
    public void IgnoreFormFieldNames_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.IgnoreFormFieldNames);
    }

    [Test]
    public void IgnoreHeaderNames_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.IgnoreHeaderNames);
    }

    [Test]
    public void IgnoreCookieNames_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.IgnoreCookieNames);
    }

    [Test]
    public void IgnoreServerVariableNames_EmptyByDefault()
    {
      Assert.IsEmpty(RaygunSettings.Settings.IgnoreServerVariableNames);
    }
  }
}
