namespace spotifete.Models;

public class SlimListeningSession
{
    public SlimListeningSession(string _name, string _joinId)
    {
        Name = _name;
        JoinId = _joinId;
    }

    public string Name { get; set; }
    public string JoinId { get; set; }
}