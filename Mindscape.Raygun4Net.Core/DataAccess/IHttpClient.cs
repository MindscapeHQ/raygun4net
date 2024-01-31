using System;

namespace Mindscape.Raygun4Net.Common.DataAccess
{
  public interface IHttpClient
  {
    string UploadString(Uri address, string data);
  }
}
