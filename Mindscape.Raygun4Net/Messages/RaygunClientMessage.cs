using System.Reflection;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunClientMessage
  {
    public RaygunClientMessage()
    {
#if ANDROID
      Name = "Raygun4Net.Xamarin.Android";
#elif IOS
      Name = "Raygun4Net.Xamarin.iOS";
#else
      Name = "Raygun4Net";
#endif

#if WINRT
      Version = typeof (RaygunClient).GetTypeInfo().Assembly.GetName().Version.ToString();
#elif WINDOWS_PHONE
      Version = Assembly.GetExecutingAssembly().FullName;
#else
      Version = Assembly.GetAssembly(typeof(RaygunClient)).GetName().Version.ToString();
#endif

      ClientUrl = @"https://github.com/MindscapeHQ/raygun4net";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}