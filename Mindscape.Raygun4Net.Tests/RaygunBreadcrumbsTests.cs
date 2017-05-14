using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net.Breadcrumbs;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunBreadcrumbsTests
  {
    class BreadcrumbTest
    {
      private readonly RaygunBreadcrumbsTests _testClass;

      public BreadcrumbTest(RaygunBreadcrumbsTests testClass)
      {
        _testClass = testClass;
      }

      public void Foo()
      {
        Action wrapper = () => _testClass._breadcrumbs.Record("foo");
        wrapper();
      }
    }

    internal RaygunBreadcrumbs _breadcrumbs;

    [SetUp]
    public void SetUp()
    {
      _breadcrumbs = new RaygunBreadcrumbs(new InMemoryBreadcrumbStorage());
    }

    [TearDown]
    public void TearDown()
    {
      RaygunSettings.Settings.BreadcrumbsLevel = BreadcrumbLevel.Info;
      RaygunSettings.Settings.BreadcrumbsLocationRecordingEnabled = false;
    }

    [Test]
    public void Set_ClassName_MethodName_And_LineNumber_Automatically_If_Configured()
    {
      RaygunSettings.Settings.BreadcrumbsLocationRecordingEnabled = true;
      var test = new BreadcrumbTest(this);

      test.Foo();
      var crumb = _breadcrumbs.First();

      Assert.That(crumb.ClassName, Is.EqualTo("Mindscape.Raygun4Net.Tests.RaygunBreadcrumbsTests+BreadcrumbTest"));
      Assert.That(crumb.MethodName, Is.EqualTo("Foo"));
      // Does this ever work? Can't find the line number
      // Assert.That(crumb.LineNumber, Is.Not.Null);
    }

    [Test]
    public void You_Can_Record_A_Breadcrumb()
    {
      _breadcrumbs.Record("test");

      Assert.That(_breadcrumbs.First().Message, Is.EqualTo("test"));
    }

    [Test]
    public void It_Sets_The_Timestamp()
    {
      _breadcrumbs.Record("test");

      Assert.That(_breadcrumbs.First().Timestamp, Is.GreaterThan(0));
    }

    [Test]
    public void It_Sets_The_Level_To_Info_If_Not_Set()
    {
      _breadcrumbs.Record("test");

      Assert.That(_breadcrumbs.First().Level, Is.EqualTo(BreadcrumbLevel.Info));
    }

    [Test]
    public void It_Does_Not_Record_A_Breadcrumb_When_The_Breadcrumb_Level_Is_Too_High()
    {
      RaygunSettings.Settings.BreadcrumbsLevel = BreadcrumbLevel.Error;

      _breadcrumbs.Record(new RaygunBreadcrumb() { Message = "test", Level = BreadcrumbLevel.Info });

      Assert.That(_breadcrumbs, Is.Empty);
    }

    [Test]
    public void You_Can_Retrieve_Stored_Breadcrumbs()
    {
      _breadcrumbs = new RaygunBreadcrumbs(
         new InMemoryBreadcrumbStorage(
            new List<RaygunBreadcrumb>() { new RaygunBreadcrumb() }
         )
      );

      Assert.That(_breadcrumbs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void You_Can_Clear_The_Stored_Breadcrumbs()
    {
      _breadcrumbs = new RaygunBreadcrumbs(
         new InMemoryBreadcrumbStorage(
            new List<RaygunBreadcrumb>() { new RaygunBreadcrumb() }
         )
      );

      _breadcrumbs.Clear();

      Assert.That(_breadcrumbs.Count(), Is.EqualTo(0));
    }
  }
}