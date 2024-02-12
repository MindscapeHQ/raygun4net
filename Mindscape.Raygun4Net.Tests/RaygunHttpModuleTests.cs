using System;
using System.Web;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunHttpModuleTests
  {
    private FakeRaygunHttpModule _module;

    [SetUp]
    public void SetUp()
    {
      _module = new FakeRaygunHttpModule();
      _module.Init(new System.Web.HttpApplication());
    }

    [Test]
    public void CanSend()
    {
      Assert.That(_module.ExposeCanSend(new NullReferenceException()), Is.True);
      Assert.That(_module.ExposeCanSend(new HttpException(404, "Not Found")), Is.True);
    }

    [Test]
    public void CanSend_ExcludeStatusCodeButNotHttpException()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.That(_module.ExposeCanSend(new InvalidOperationException()), Is.True);

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    [Test]
    public void CanSend_ExcludeDifferentStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.That(_module.ExposeCanSend(new HttpException(500, "Error message")), Is.True);

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    [Test]
    public void CanNotSend_ExcludeStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.That(_module.ExposeCanSend(new HttpException(404, "Not Found")), Is.False);

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    // GetRaygunClient tests

    [Test]
    public void GetRaygunClient()
    {
      Assert.That(_module.ExposeGetRaygunClient(new HttpApplication()).User, Is.Null);
    }

    [Test]
    public void GetCustomizedRaygunClient()
    {
      Assert.That("TestUser", Is.EqualTo(_module.ExposeGetRaygunClient(new FakeHttpApplication()).User)); // As set in FakeHttpApplication
    }
  }
}
