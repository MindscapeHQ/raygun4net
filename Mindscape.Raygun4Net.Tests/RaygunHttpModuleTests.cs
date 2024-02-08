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
      Assert.IsTrue(_module.ExposeCanSend(new NullReferenceException()));
      Assert.IsTrue(_module.ExposeCanSend(new HttpException(404, "Not Found")));
    }

    [Test]
    public void CanSend_ExcludeStatusCodeButNotHttpException()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(_module.ExposeCanSend(new InvalidOperationException()));

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    [Test]
    public void CanSend_ExcludeDifferentStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(_module.ExposeCanSend(new HttpException(500, "Error message")));

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    [Test]
    public void CanNotSend_ExcludeStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(_module.ExposeCanSend(new HttpException(404, "Not Found")));

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    // GetRaygunClient tests

    [Test]
    public void GetRaygunClient()
    {
      Assert.IsNull(_module.ExposeGetRaygunClient(new HttpApplication()).User);
    }

    [Test]
    public void GetCustomizedRaygunClient()
    {
      Assert.AreEqual("TestUser", _module.ExposeGetRaygunClient(new FakeHttpApplication()).User); // As set in FakeHttpApplication
    }
  }
}
