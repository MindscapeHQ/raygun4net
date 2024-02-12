using System;
using System.Web;
using System.Web.Mvc;
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
      Assert.That(_module.ExposeCanSend(new NullReferenceException()), Is.True);
      Assert.That(_module.ExposeCanSend(new HttpException(404, "Not Found")), Is.True);
    }

    [Test]
    public void CanSend_ExcludeStatusCodeButNotHttpException()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.That(_module.ExposeCanSend(new InvalidOperationException()), Is.True);
    }

    [Test]
    public void CanSend_ExcludeDifferentStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.That(_module.ExposeCanSend(new HttpException(500, "Error message")), Is.True);
    }

    [Test]
    public void CanNotSend_ExcludeStatusCode()
    {
      RaygunSettings.Settings.ExcludeHttpStatusCodesList = "404";
      _module.Init(new System.Web.HttpApplication());

      Assert.That(_module.ExposeCanSend(new HttpException(404, "Not Found")), Is.False);
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

    // Global filter tests

    [Test]
    public void AddRaygunFilterIfHandleErrorAttributeIsPresent()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      Assert.That(1, Is.EqualTo(GlobalFilters.Filters.Count));
      Assert.That(HasRaygunFilter, Is.False);

      _module.Init(new System.Web.HttpApplication());

      Assert.That(HasRaygunFilter, Is.True);
      Assert.That(2, Is.EqualTo(GlobalFilters.Filters.Count));
    }

    [Test]
    public void DoNotAddRaygunFilterIfNoFiltersPresent()
    {
      Assert.That(0, Is.EqualTo(GlobalFilters.Filters.Count));

      _module.Init(new System.Web.HttpApplication());

      Assert.That(HasRaygunFilter, Is.False);
      Assert.That(0, Is.EqualTo(GlobalFilters.Filters.Count));
    }

    [Test]
    public void CanAddRaygunFilterIfMoreThanHandleErrorAttributeIsPresent()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      GlobalFilters.Filters.Add(new FakeFilterAttribute());
      Assert.That(2, Is.EqualTo(GlobalFilters.Filters.Count));
      Assert.That(HasRaygunFilter, Is.False);

      _module.Init(new System.Web.HttpApplication());

      Assert.That(HasRaygunFilter, Is.True);
      Assert.That(3, Is.EqualTo(GlobalFilters.Filters.Count));
    }

    [Test]
    public void DoNotAddRaygunFilterIfHandleErrorAttributeIsNotPresent()
    {
      GlobalFilters.Filters.Add(new FakeFilterAttribute());
      Assert.That(1, Is.EqualTo(GlobalFilters.Filters.Count));
      Assert.That(HasRaygunFilter, Is.False);

      _module.Init(new System.Web.HttpApplication());

      Assert.That(HasRaygunFilter, Is.False);
      Assert.That(1, Is.EqualTo(GlobalFilters.Filters.Count));
    }

    [Test]
    public void CanNotAddMultipleRaygunFilters()
    {
      GlobalFilters.Filters.Add(new HandleErrorAttribute());
      Assert.That(1, Is.EqualTo(GlobalFilters.Filters.Count));
      Assert.That(HasRaygunFilter, Is.False);

      _module.Init(new System.Web.HttpApplication());
      _module.Init(new System.Web.HttpApplication());

      Assert.That(HasRaygunFilter, Is.True);
      Assert.That(2, Is.EqualTo(GlobalFilters.Filters.Count));
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
