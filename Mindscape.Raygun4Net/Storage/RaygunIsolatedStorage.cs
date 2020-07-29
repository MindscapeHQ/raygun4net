using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Storage
{
  public class RaygunIsolatedStorage : IRaygunOfflineStorage
  {
    private const string RaygunBaseDirectory = "RaygunOfflineStorage";
    public bool Store(string message, string apiKey)
    {
      using (IsolatedStorageFile isolatedStorage = GetIsolatedStorageScope())
      {
        string[] directories = isolatedStorage.GetDirectoryNames("*");

        if (!FileExists(directories, RaygunBaseDirectory))
        {
          isolatedStorage.CreateDirectory(RaygunBaseDirectory);
        }

        int number = 1;
        string[] files = isolatedStorage.GetFileNames(RaygunBaseDirectory + "\\*.txt");

        while (true)
        {
          bool exists = FileExists(files, "RaygunErrorMessage" + number + ".txt");

          if (!exists)
          {
            string nextFileName = "RaygunErrorMessage" + (number + 1) + ".txt";
            exists = FileExists(files, nextFileName);
            if (exists)
            {
              isolatedStorage.DeleteFile(RaygunBaseDirectory + "\\" + nextFileName);
            }
            break;
          }
          number++;
        }

        if (number == 11)
        {
          string firstFileName = "RaygunErrorMessage1.txt";

          if (FileExists(files, firstFileName))
          {
            isolatedStorage.DeleteFile(RaygunBaseDirectory + "\\" + firstFileName);
          }
        }
        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(RaygunBaseDirectory + "\\RaygunErrorMessage" + number + ".txt", FileMode.OpenOrCreate, FileAccess.Write, isolatedStorage))
        {
          using (StreamWriter writer = new StreamWriter(isoStream, Encoding.Unicode))
          {
            writer.Write(message);
            writer.Flush();
            writer.Close();
          }
        }
      }

      return true;
    }

    public IList<IRaygunFile> FetchAll(string apiKey)
    {
      var files = new List<IRaygunFile>();

      using (IsolatedStorageFile isolatedStorage = GetIsolatedStorageScope())
      {
        string[] directories = isolatedStorage.GetDirectoryNames("*");

        if (FileExists(directories, RaygunBaseDirectory))
        {
          string[] fileNames = isolatedStorage.GetFileNames(RaygunBaseDirectory + "\\*.txt");

          foreach (string name in fileNames)
          {
            IsolatedStorageFileStream isoFileStream = new IsolatedStorageFileStream(RaygunBaseDirectory + "\\" + name, FileMode.Open, isolatedStorage);

            using (StreamReader reader = new StreamReader(isoFileStream))
            {
              string contents = reader.ReadToEnd();
              files.Add(new RaygunFile()
              {
                Name = name,
                Contents = contents
              });
            }
          }
        }
      }

      return files;
    }

    public bool Remove(string name, string apiKey)
    {
      throw new System.NotImplementedException();
    }

    private IsolatedStorageFile GetIsolatedStorageScope()
    {
      if (AppDomain.CurrentDomain != null && AppDomain.CurrentDomain.ActivationContext != null)
      {
        return IsolatedStorageFile.GetUserStoreForApplication();
      }
      else
      {
        return IsolatedStorageFile.GetUserStoreForAssembly();
      }
    }

    private bool FileExists(string[] files, string fileName)
    {
      foreach (string str in files)
      {
        if (fileName.Equals(str))
        {
          return true;
        }
      }
      return false;
    }
  }
}