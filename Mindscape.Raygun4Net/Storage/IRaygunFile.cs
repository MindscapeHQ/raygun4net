namespace Mindscape.Raygun4Net.Storage
{
  public interface IRaygunFile
  {
    /// <summary>
    /// The filename as seen in local storage.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The raw data stored locally.
    /// </summary>
    string Contents { get; }
  }
}