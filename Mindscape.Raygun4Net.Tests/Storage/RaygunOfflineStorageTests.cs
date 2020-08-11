using System.Linq;
using Mindscape.Raygun4Net.Storage;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests.Storage
{
  [TestFixture]
  public class RaygunOfflineStorageTests
  {
    private IRaygunOfflineStorage _storage;
    private const string TestApiKey = "DummyApiKey";

    [SetUp]
    public void SetUp()
    {
      _storage = new RaygunIsolatedStorage();
    }

    [TearDown]
    public void TearDown()
    {
      // Clear all files from storage.
      var files = _storage.FetchAll(TestApiKey);
      foreach (var file in files)
      {
        _storage.Remove(file.Name, TestApiKey);
      }
    }

    [Test]
    public void Store_UsingInvalidParamValues_ReturnFalse()
    {
      Assert.IsFalse(_storage.Store(null, TestApiKey, 1));
      Assert.IsFalse(_storage.Store("", TestApiKey, 1));
    }

    [Test]
    public void FetchAll_UsingInvalidParamValues_ReturnEmptyResult()
    {
      Assert.IsEmpty(_storage.FetchAll(null));
      Assert.IsEmpty(_storage.FetchAll(""));
    }

    [Test]
    public void Remove_UsingInvalidParamValues_ReturnFalse()
    {
      Assert.IsFalse(_storage.Remove(null, TestApiKey));
      Assert.IsFalse(_storage.Remove("", TestApiKey));
    }

    [Test]
    public void Store_WriteASingleMessageToStorage_OneMessageIsAvailableFromStorage()
    {
      // Ensure there are no files in storage
      var files = _storage.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(0));

      // Save one message to storage
      _storage.Store("DummyData", TestApiKey, 1);

      files = _storage.FetchAll(TestApiKey);

      // Ensure only one file was created
      Assert.That(files.Count, Is.EqualTo(1));
      Assert.That(files.First().Contents, Is.EqualTo("DummyData"));
    }

    [Test]
    public void Store_WriteMultipleMessagesToStorage_MaxReportsLimitIsRespected()
    {
      // Ensure there are no files in storage
      var files = _storage.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(0));

      const int maxReportsStored = 1;

      // Save two messages to storage
      _storage.Store("DummyData1", TestApiKey, maxReportsStored);
      _storage.Store("DummyData2", TestApiKey, maxReportsStored);

      files = _storage.FetchAll(TestApiKey);

      // Ensure only one file was created
      Assert.That(files.Count, Is.EqualTo(1));
      Assert.That(files.First().Contents, Is.EqualTo("DummyData1"));
    }

    [Test]
    public void Remove_TwoMessagesStoredAndOneRemoved_OnlyOneMessageRemainsInStorage()
    {
      // Ensure there are no files in storage
      var files = _storage.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(0));

      const int maxReportsStored = 2;

      // Save two messages to storage
      Assert.IsTrue(_storage.Store("DummyData1", TestApiKey, maxReportsStored));
      Assert.IsTrue(_storage.Store("DummyData2", TestApiKey, maxReportsStored));

      files = _storage.FetchAll(TestApiKey);

      // Ensure two files were created
      Assert.That(files.Count, Is.EqualTo(2));

      // Remove the first file
      Assert.IsTrue(_storage.Remove(files.First().Name, TestApiKey));

      // Ensure only one file remains
      files = _storage.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(1));
    }
  }
}