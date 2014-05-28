namespace Mindscape.Raygun4Net
{
  /// <summary>
  /// This is used by the HttpModule to generate RaygunClients. If you wish to
  /// use it then, implement this interface with your HttpApplication.
  /// </summary>
  public interface IRaygunApplication
  {
    /// <summary>
    /// This is called by the HttpModule to get a customised RaygunClient.
    /// </summary>
    RaygunClient GenerateRaygunClient();
  }
}
