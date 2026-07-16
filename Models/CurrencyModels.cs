namespace SMASSB.Models;

public class CurrencyModels {
    
    public record CurrencyRequest(ulong UserId, int Amount);
    public record CurrencyResult(ulong UserId, int NewBalance);
}