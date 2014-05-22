using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net;
using NUnit.Framework;

namespace Mindscape.Raygun4Net2.Tests
{
  [TestFixture]
  public class RaygunHttpModuleTests
  {
    private RaygunHttpModule _module;

    [SetUp]
    public void SetUp()
    {
      _module = new RaygunHttpModule();
      _module.Init(new System.Web.HttpApplication());
    }

    [Test]
    public void CanSend()
    {
      Assert.IsTrue(_module.CanSend(new NullReferenceException()));
      Assert.IsTrue(_module.CanSend(new HttpException(404, "Not Found")));
    }

    [Test]
    public void CanSend_ExcludeStatusCodeButNotHttpException()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(_module.CanSend(new InvalidOperationException()));

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    [Test]
    public void CanSend_ExcludeDifferentStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(_module.CanSend(new HttpException(500, "Error message")));

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }

    [Test]
    public void CanNotSend_ExcludeStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(_module.CanSend(new HttpException(404, "Not Found")));

      RaygunSettings.Settings.ExcludeHttpStatusCodesList = ""; // Revert for other tests
    }
  }
}
