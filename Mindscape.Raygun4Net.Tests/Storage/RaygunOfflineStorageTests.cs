using System.Linq;
using Mindscape.Raygun4Net.Storage;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests.Storage
{
  [TestFixture]
  public class RaygunOfflineStorageTests
  {
    private IRaygunOfflineStorage _storageOne;
    private IRaygunOfflineStorage _storageTwo;

    private const string TestApiKey = "DummyApiKey";

    [SetUp]
    public void SetUp()
    {
      _storageOne = new IsolatedRaygunOfflineStorage();
      _storageTwo = new IsolatedRaygunOfflineStorage();
    }

    [TearDown]
    public void TearDown()
    {
      // Clear all files from storage.
      var filesOne = _storageOne.FetchAll(TestApiKey);
      foreach (var file in filesOne)
      {
        _storageOne.Remove(file.Name, TestApiKey);
      }

      var filesTwo = _storageTwo.FetchAll(TestApiKey);
      foreach (var file in filesTwo)
      {
        _storageTwo.Remove(file.Name, TestApiKey);
      }
    }

    [Test]
    public void Store_UsingInvalidMessageValue_ReturnFalse()
    {
      Assert.IsFalse(_storageOne.Store(null, TestApiKey));
      Assert.IsFalse(_storageOne.Store("", TestApiKey));
    }

    [Test]
    public void Store_UsingInvalidApiKeyValue_ReturnFalse()
    {
      Assert.IsFalse(_storageOne.Store("DummyData", null));
      Assert.IsFalse(_storageOne.Store("DummyData", ""));
    }

    [Test]
    public void FetchAll_UsingInvalidApiKeyValue_ReturnEmptyResult()
    {
      Assert.IsEmpty(_storageOne.FetchAll(null));
      Assert.IsEmpty(_storageOne.FetchAll(""));
    }

    [Test]
    public void Remove_UsingInvalidNameValue_ReturnFalse()
    {
      Assert.IsFalse(_storageOne.Remove(null, TestApiKey));
      Assert.IsFalse(_storageOne.Remove("", TestApiKey));
    }

    [Test]
    public void Remove_UsingInvalidApiKeyValue_ReturnFalse()
    {
      Assert.IsFalse(_storageOne.Remove("DummyName", null));
      Assert.IsFalse(_storageOne.Remove("DummyName", ""));
    }

    [Test]
    public void Store_WriteASingleMessageToStorage_OneMessageIsAvailableFromStorage()
    {
      // Ensure there are no files in storage.
      var files = _storageOne.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(0));

      RaygunSettings.Settings.MaxCrashReportsStoredOffline = 1;

      // Save one message to storage.
      _storageOne.Store("DummyData", TestApiKey);

      files = _storageOne.FetchAll(TestApiKey);

      // Ensure only one file was created.
      Assert.That(files.Count, Is.EqualTo(1));
      Assert.That(files.First().Contents, Is.EqualTo("DummyData"));
    }

    [Test]
    public void Store_WriteMultipleMessagesToStorage_MaxReportsLimitIsRespected()
    {
      // Ensure there are no files in storage.
      var files = _storageOne.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(0));

      RaygunSettings.Settings.MaxCrashReportsStoredOffline = 1;

      // Save two messages to storage.
      _storageOne.Store("DummyData1", TestApiKey);
      _storageOne.Store("DummyData2", TestApiKey);

      files = _storageOne.FetchAll(TestApiKey);

      // Ensure only one file was created.
      Assert.That(files.Count, Is.EqualTo(1));
      Assert.That(files.First().Contents, Is.EqualTo("DummyData1"));
    }

    [Test]
    public void Remove_TwoMessagesStoredAndOneRemoved_OnlyOneMessageRemainsInStorage()
    {
      // Ensure there are no files in storage.
      var files = _storageOne.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(0));

      RaygunSettings.Settings.MaxCrashReportsStoredOffline = 2;

      // Save two messages to storage.
      Assert.IsTrue(_storageOne.Store("DummyData1", TestApiKey));
      Assert.IsTrue(_storageOne.Store("DummyData2", TestApiKey));

      files = _storageOne.FetchAll(TestApiKey);

      // Ensure two files were created.
      Assert.That(files.Count, Is.EqualTo(2));

      // Remove the first file.
      Assert.IsTrue(_storageOne.Remove(files.First().Name, TestApiKey));

      // Ensure only one file remains.
      files = _storageOne.FetchAll(TestApiKey);
      Assert.That(files.Count, Is.EqualTo(1));
    }

    [Test]
    public void Store_StoreUnderFirstKeyAndFetchWithSecondKey_NoFilesFoundForSecondKey()
    {
      const string apiKeyOne = "KEY_ONE";
      const string apiKeyTwo = "KEY_TWO";

      // Ensure there are no files in storage under the first API key.
      Assert.That(_storageOne.FetchAll(apiKeyOne).Count, Is.EqualTo(0));

      // Ensure there are no files in storage under the second API key.
      Assert.That(_storageTwo.FetchAll(apiKeyTwo).Count, Is.EqualTo(0));

      RaygunSettings.Settings.MaxCrashReportsStoredOffline = 1;

      // Save one messages to storage under the first API key.
      _storageOne.Store("DummyData1", apiKeyOne);

      // There should be one file under the first API key.
      Assert.That(_storageOne.FetchAll(apiKeyOne).Count, Is.EqualTo(1));
      Assert.That(_storageTwo.FetchAll(apiKeyTwo).Count, Is.EqualTo(0));
    }
  }
}