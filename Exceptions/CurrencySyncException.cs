namespace SMASSB.Exceptions;

/// <summary>
/// Triggered whenever this bot can't talk to the SANGOFES bot.
/// </summary>
public class CurrencySyncException : Exception {
    public string UserName { get; }

    public CurrencySyncException(string userName, string message, Exception inner) : base(message, inner) {
        UserName = userName;
    }
}