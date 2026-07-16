namespace SMASSB.Exceptions;

public class CurrencySyncException : Exception {
    public string UserName { get; }

    public CurrencySyncException(string userName, string message, Exception inner) : base(message, inner) {
        UserName = userName;
    }
}