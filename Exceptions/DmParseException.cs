namespace SMASSB.Exceptions;

/// <summary>
/// Triggered whenever we can't read someone's DMs.
/// </summary>
public class DmParseException : Exception {
    
    public string UserName { get; }
    
    public DmParseException(string userName) : base($"Failed to parse message in '{userName}'s DMs'.") {
        UserName = userName;
    }
}