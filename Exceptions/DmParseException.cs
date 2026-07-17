namespace SMASSB.Exceptions;

public class DmParseException : Exception {
    
    public string UserName { get; }
    
    public DmParseException(string userName) : base($"Failed to parse message in '{userName}'s DMs'.") {
        UserName = userName;
    }
}