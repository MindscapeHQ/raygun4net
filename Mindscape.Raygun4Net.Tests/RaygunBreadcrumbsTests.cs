using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net.Breadcrumbs;
using NUnit.Framework;
using Tests.Model;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunBreadcrumbsTests
  {
    private RaygunBreadcrumbs _breadcrumbs;

    [SetUp]
    public void SetUp()
    {
      _breadcrumbs = new RaygunBreadcrumbs(new InMemoryBreadcrumbStorage());
    }

    [TearDown]
    public void TearDown()
    {
      RaygunSettings.Settings.BreadcrumbsLevel = RaygunBreadcrumbLevel.Info;
      RaygunSettings.Settings.BreadcrumbsLocationRecordingEnabled = false;
    }

    [Test]
    public void Set_ClassName_MethodName_And_LineNumber_Automatically_If_Configured()
    {
      RaygunSettings.Settings.BreadcrumbsLocationRecordingEnabled = true;
      var test = new BreadcrumbTestRecorder(_breadcrumbs);

      test.Record();
      var crumb = _breadcrumbs.First();

      Assert.That(crumb.ClassName, Is.EqualTo("Tests.Model.BreadcrumbTestRecorder"));
      Assert.That(crumb.MethodName, Is.EqualTo("Record"));
    }

    [Test]
    public void You_Can_Record_A_Breadcrumb()
    {
      _breadcrumbs.Record(new RaygunBreadcrumb() { Message = "test" });

      Assert.That(_breadcrumbs.First().Message, Is.EqualTo("test"));
    }

    [Test]
    public void It_Sets_The_Timestamp()
    {
      _breadcrumbs.Record(new RaygunBreadcrumb() { Message = "test" });

      Assert.That(_breadcrumbs.First().Timestamp, Is.GreaterThan(0));
    }

    [Test]
    public void It_Sets_The_Level_To_Info_If_Not_Set()
    {
      _breadcrumbs.Record(new RaygunBreadcrumb() { Message = "test" });

      Assert.That(_breadcrumbs.First().Level, Is.EqualTo(RaygunBreadcrumbLevel.Info));
    }

    [Test]
    public void It_Does_Not_Record_A_Breadcrumb_When_The_Breadcrumb_Level_Is_Too_High()
    {
      RaygunSettings.Settings.BreadcrumbsLevel = RaygunBreadcrumbLevel.Error;

      _breadcrumbs.Record(new RaygunBreadcrumb() { Message = "test", Level = RaygunBreadcrumbLevel.Info });

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