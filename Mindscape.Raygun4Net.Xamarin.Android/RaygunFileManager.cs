using Java.IO;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using System;
using Android.Content;
using Android.Runtime;

namespace Mindscape.Raygun4Net
{
  public class RaygunFileManager
  {
    private const int MaxStoredReports = 64;

    public RaygunFileManager(){}

    public void SaveMessage(RaygunMessage message, Context context)
    {
      try
      {
        var messageJson = SimpleJson.SerializeObject(message);

        if (context != null)
        {
          using (File dir = context.GetDir("RaygunIO", FileCreationMode.Private))
          {
            int number = 1;
            string[] files = dir.List();
            while (true)
            {
              bool exists = FileExists(files, "RaygunErrorMessage" + number + ".txt");
              if (!exists)
              {
                string nextFileName = "RaygunErrorMessage" + (number + 1) + ".txt";
                exists = FileExists(files, nextFileName);
                if (exists)
                {
                  DeleteFile(dir, nextFileName);
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
                DeleteFile(dir, firstFileName);
              }
            }

            using (File file = new File(dir, "RaygunErrorMessage" + number + ".txt"))
            {
              using (FileOutputStream stream = new FileOutputStream(file))
              {
                stream.Write(Encoding.ASCII.GetBytes(messageJson));
                stream.Flush();
                stream.Close();
              }
            }

            RaygunLogger.Debug("Saved message: " + "RaygunErrorMessage" + number + ".txt");
            RaygunLogger.Debug("File Count: " + dir.List().Length);
          }
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Error(string.Format("Error saving message to isolated storage {0}", ex.Message));
      }
    }

    public void SendStoredMessages(Context context)
    {
      try
      {
        using (File dir = context.GetDir("RaygunIO", FileCreationMode.Private))
        {
          File[] files = dir.ListFiles();
          foreach (File file in files)
          {
            if (file.Name.StartsWith("RaygunErrorMessage"))
            {
              using (FileInputStream stream = new FileInputStream(file))
              {
                using (InputStreamInvoker isi = new InputStreamInvoker(stream))
                {
                  using (InputStreamReader streamReader = new Java.IO.InputStreamReader(isi))
                  {
                    using (BufferedReader bufferedReader = new BufferedReader(streamReader))
                    {
                      StringBuilder stringBuilder = new StringBuilder();
                      string line;
                      while ((line = bufferedReader.ReadLine()) != null)
                      {
                        stringBuilder.Append(line);
                      }
                      bool success = RaygunClient.Current.SendMessage(stringBuilder.ToString());
                      // If just one message fails to send, then don't delete the message, and don't attempt sending anymore until later.
                      if (!success)
                      {
                        return;
                      }

                      RaygunLogger.Debug("Sent " + file.Name);
                    }
                  }
                }
              }
              file.Delete();
            }
          }
          if (dir.List().Length == 0)
          {
            if (files.Length > 0)
            {
              RaygunLogger.Debug("Successfully sent all pending messages");
            }
            dir.Delete();
          }
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Error(string.Format("Error sending stored messages to Raygun.io {0}", ex.Message));
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

    private void DeleteFile(File dir, string fileName)
    {
      File[] files = dir.ListFiles();
      foreach (File file in files)
      {
        if (fileName.Equals(file.Name))
        {
          file.Delete();
          return;
        }
      }
    }
  }
}
