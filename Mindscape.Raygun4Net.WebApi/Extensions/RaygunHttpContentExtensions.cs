using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.WebApi
{
  internal static class RaygunHttpContentExtensions
  {
    internal static string ReadAsString(this HttpContent httpContent)
    {
      try
      {
        var task = httpContent.ReadAsStreamAsync();
        task.Wait();
        var stream = task.Result;
        if (stream != null && stream.CanSeek)
        {
          var lengthToRead = (int)(stream.Length < 4096 ? stream.Length : 4096);
          var buffer = new byte[lengthToRead];

          var stringTask = stream.ReadAsync(buffer, 0, lengthToRead);
          stringTask.Wait();

          var content = Encoding.UTF8.GetString(buffer);
          stream.Seek(0, System.IO.SeekOrigin.Begin);
          if (!string.IsNullOrEmpty(content))
          {
            return content;
          }
        }
      }
      catch { }

      return null;
    }
  }
}
