using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunSettingsTests
  {
    [Test]
    public void Apikey_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.ApiKey, Is.Empty);
    }

    [Test]
    public void ApiEndPoint_DefaultValue()
    {
      Assert.That("https://api.raygun.com/entries", Is.EqualTo(RaygunSettings.Settings.ApiEndpoint.AbsoluteUri));
    }

    [Test]
    public void MediumTrust_FalseByDefault()
    {
      Assert.That(RaygunSettings.Settings.MediumTrust, Is.False);
    }

    [Test]
    public void ThrowOnError_FalseByDefault()
    {
      Assert.That(RaygunSettings.Settings.ThrowOnError, Is.False);
    }

    [Test]
    public void ExcludeHttpStatusCodesList_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.ExcludeHttpStatusCodesList, Is.Empty);
    }

    [Test]
    public void ExcludeErrorsFromLocal_FalseByDefault()
    {
      Assert.That(RaygunSettings.Settings.ExcludeErrorsFromLocal, Is.False);
    }

    [Test]
    public void IgnoreFormFieldNames_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.IgnoreFormFieldNames, Is.Empty);
    }

    [Test]
    public void IgnoreHeaderNames_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.IgnoreHeaderNames, Is.Empty);
    }

    [Test]
    public void IgnoreCookieNames_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.IgnoreCookieNames, Is.Empty);
    }

    [Test]
    public void IgnoreServerVariableNames_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.IgnoreServerVariableNames, Is.Empty);
    }

    [Test]
    public void IsRawDataIgnored_FalseByDefault()
    {
      Assert.That(RaygunSettings.Settings.IsRawDataIgnored, Is.False);
    }

    [Test]
    public void ApplicationVersion_EmptyByDefault()
    {
      Assert.That(RaygunSettings.Settings.ApplicationVersion, Is.Empty);
    }

    [Test]
    public void IsNotReadOnly()
    {
      Assert.That(RaygunSettings.Settings.IsReadOnly(), Is.False);
    }
  }
}
