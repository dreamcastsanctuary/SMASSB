namespace SMASSB.Exceptions;

public class MessageSendException : Exception
{
    public string UserName { get; }

    public MessageSendException(string userName) : base($"Failed to send a message to user '{userName}'.") {
        UserName = userName;
    }

    public MessageSendException(string userName, string message) : base(message) {
        UserName = userName;
    }

    public MessageSendException(string userName, string message, Exception innerException) : base(message, innerException) {
        UserName = userName;
    }
    
    public MessageSendException(string userName, Exception innerException) : base($"Failed to send a message to user '{userName}'.", innerException) {
        UserName = userName;
    }
}