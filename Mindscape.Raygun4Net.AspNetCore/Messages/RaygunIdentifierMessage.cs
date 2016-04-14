namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunIdentifierMessage
  {
    public RaygunIdentifierMessage(string user)
    {
      Identifier = user;
    }

    /// <summary>
    /// Unique Identifier for this user. Set this to the identifier you use internally to look up users,
    /// or a correlation id for anonymous users if you have one. It doesn't have to be unique, but we will
    /// treat any duplicated values as the same user. If you use the user's email address as the identifier,
    /// enter it here as well as the Email field.
    /// </summary>
    public string Identifier { get; set; }

    /// <summary>
    /// Flag indicating whether a user is anonymous or not.
    /// </summary>
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// User's full name. If you are going to set any names, you should probably set this one too.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Device Identifier. Could be used to identify users across apps.
    /// </summary>
    public string UUID { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunIdentifierMessage: Identifier={0}, IsAnonymous={1}, Email={2}, FullName={3}, FirstName={4}, UUID={5}]", Identifier, IsAnonymous, Email, FullName, FirstName, UUID);
    }
  }
}
