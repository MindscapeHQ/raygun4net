using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  /// <summary>
  /// Represents an initializer for <see cref="RaygunMessage"/> objects.
  /// </summary>
  /// <remarks>
  /// The Raygun Clients use registered <see cref="IRaygunMessageInitializer"/>
  /// to automatically add additional properties to <see cref="RaygunMessage"/> objects.
  /// </remarks>
  public interface IRaygunMessageInitializer
  {
    /// <summary>
    /// Initializes properties of the specified <see cref="RaygunMessage"/> object.
    /// </summary>
    /// <param name="raygunMessage">The <see cref="RaygunMessage"/> to initialize.</param>
    void Initialize(RaygunMessage raygunMessage);
  }
}