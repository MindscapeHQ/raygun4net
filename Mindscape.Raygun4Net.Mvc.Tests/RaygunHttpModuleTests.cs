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

    // Global filter tests

    [Test]
    public void AddRaygunFilterIfOnlyHandleErrorAttributeIsPresent()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      Assert.AreEqual(1, GlobalFilters.Filters.Count);
      Assert.IsFalse(HasRaygunFilter);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsTrue(HasRaygunFilter);
      Assert.AreEqual(2, GlobalFilters.Filters.Count);

      GlobalFilters.Filters.Clear(); // Revert for other tests
    }

    [Test]
    public void DoNotAddRaygunFilterIfNoFiltersPresent()
    {
      Assert.AreEqual(0, GlobalFilters.Filters.Count);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(HasRaygunFilter);
      Assert.AreEqual(0, GlobalFilters.Filters.Count);

      GlobalFilters.Filters.Clear(); // Revert for other tests
    }

    [Test]
    public void DoNotAddRaygunFilterIfMoreThanHandleErrorAttributeIsPresent()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      GlobalFilters.Filters.Add(new FakeFilterAttribute());
      Assert.AreEqual(2, GlobalFilters.Filters.Count);
      Assert.IsFalse(HasRaygunFilter);

      _module.Init(new System.Web.HttpApplication());

      Assert.IsFalse(HasRaygunFilter);
      Assert.AreEqual(2, GlobalFilters.Filters.Count);

      GlobalFilters.Filters.Clear(); // Revert for other tests
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

      GlobalFilters.Filters.Clear(); // Revert for other tests
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

      GlobalFilters.Filters.Clear(); // Revert for other tests
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
