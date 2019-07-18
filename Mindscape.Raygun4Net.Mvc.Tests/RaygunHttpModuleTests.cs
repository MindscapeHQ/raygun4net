using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Mindscape.Raygun4Net;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Mvc.Tests
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

    [TearDown]
    public void TearDown()
    {
      GlobalFilters.Filters.Clear();
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "";
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
    }

    [Test]
    public void CanSend_ExcludeDifferentStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(_module.ExposeCanSend(new HttpException(500, "Error message")));
    }

    [Test]
    public void CanNotSend_ExcludeStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(_module.ExposeCanSend(new HttpException(404, "Not Found")));
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

    // Global filter tests

    [Test]
    public void AddRaygunFilterIfHandleErrorAttributeIsPresent()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      Assert.AreEqual(1, GlobalFilters.Filters.Count);
      Assert.IsFalse(HasRaygunFilter);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(HasRaygunFilter);
      Assert.AreEqual(2, GlobalFilters.Filters.Count);
    }

    [Test]
    public void DoNotAddRaygunFilterIfNoFiltersPresent()
    {
      Assert.AreEqual(0, GlobalFilters.Filters.Count);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(HasRaygunFilter);
      Assert.AreEqual(0, GlobalFilters.Filters.Count);
    }

    [Test]
    public void CanAddRaygunFilterIfMoreThanHandleErrorAttributeIsPresent()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      GlobalFilters.Filters.Add(new FakeFilterAttribute());
      Assert.AreEqual(2, GlobalFilters.Filters.Count);
      Assert.IsFalse(HasRaygunFilter);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(HasRaygunFilter);
      Assert.AreEqual(3, GlobalFilters.Filters.Count);
    }

    [Test]
    public void DoNotAddRaygunFilterIfHandleErrorAttributeIsNotPresent()
    {
      GlobalFilters.Filters.Add(new FakeFilterAttribute());
      Assert.AreEqual(1, GlobalFilters.Filters.Count);
      Assert.IsFalse(HasRaygunFilter);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(HasRaygunFilter);
      Assert.AreEqual(1, GlobalFilters.Filters.Count);
    }

    [Test]
    public void CanNotAddMultipleRaygunFilters()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      Assert.AreEqual(1, GlobalFilters.Filters.Count);
      Assert.IsFalse(HasRaygunFilter);

      _module.Init(new System.Web.HttpApplication());
      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(HasRaygunFilter);
      Assert.AreEqual(2, GlobalFilters.Filters.Count);
    }

    private static bool HasRaygunFilter
    {
      get
      {
        foreach (Filter filter in GlobalFilters.Filters)
        {
          if (filter.Instance is RaygunExceptionFilterAttribute)
          {
            return true;
          }
        }
        return false;
      }
    }
  }
}
