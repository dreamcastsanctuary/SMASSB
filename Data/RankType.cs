namespace SMASSB.Data;

public enum RankType {
    Kō = 15,
    NiShi = 250,
    ItShi = 500,
    Shi = 750,
    SaSō = 1000,
    NiSō = 1250,
    ItSō = 1500,
    Sō = 10000,
    JUi = 50000,
    SaUi = 50000,
    NiUi = 50000,
    ItUi = 50000,
    SaSa = 50000,
    NiSa = 50000,
    ItSa = 50000
}

public static class RankTypeMethods {

    private static string GetFullRank(this Enum rank) {
        
        var rankInfo = rank.GetType().GetField(rank.ToString());

        switch (rankInfo?.Name) {
            case "Kō":
                return "Kōhosei";
            case "NiShi":
                return "Nitō Shi";
            case "ItShi":
                return "Ittō Shi";
            case "Shi":
                return "Shichō";
            case "SaSō":
                return "Santō Sō";
            case "NiSō":
                return "Nitō Sō";
            case "ItSō":
                return "Ittō Sō";
            case "Sō":
                return "Sōchō";
        }
        
        return rank.ToString();
    }
    
    public static RankType? ToRankType(this string fullName) {
        return Enum.GetValues<RankType>().FirstOrDefault(r => (r).GetFullRank() == fullName);
    }
}