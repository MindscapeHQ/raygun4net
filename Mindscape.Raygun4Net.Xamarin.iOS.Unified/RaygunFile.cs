using System;

namespace Mindscape.Raygun4Net
{
  public class RaygunFile
  {
    public RaygunFile(string path)
    {
      Path = path;
    }

    public string Path { get; set; }
    public string Data { get; set; }
  }
}
