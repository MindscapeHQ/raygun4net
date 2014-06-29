namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunIdentifierMessage
  {
    public RaygunIdentifierMessage(string user)
    {
      Identifier = user;
    }

    public string Identifier { get; set; }

    public bool IsAnonymous { get; set; }

    public string Email { get; set; }

    public string FullName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

  }
}
