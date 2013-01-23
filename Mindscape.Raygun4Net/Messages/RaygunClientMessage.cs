﻿using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
      Name = "Raygun4Net";
#if !WINRT
      Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString();
#else
      Version = typeof (RaygunClient).GetTypeInfo().Assembly.GetName().Version.ToString();
#endif
      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}