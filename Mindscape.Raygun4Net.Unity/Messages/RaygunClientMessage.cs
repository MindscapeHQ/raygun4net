using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    private string _name;
    private string _version;
    private string _clientUrl;

    public RaygunClientMessage()
    {
      Name = "Raygun4Net.Unity";
      Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString();
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name
    {
      get { return _name; }
      set
      {
        _name = value;
      }
    }

    public string Version
    {
      get { return _version; }
      set
      {
        _version = value;
      }
    }

    public string ClientUrl
    {
      get { return _clientUrl; }
      set
      {
        _clientUrl = value;
      }
    }
  }
}
