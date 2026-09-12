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
    SaUi = 50001,
    NiUi = 50002,
    ItUi = 50003,
    SaSa = 50004,
    NiSa = 50005,
    ItSa = 50006
}

public static class RankTypeMethods {

    public static string GetFullRank(this Enum rank) {
        
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
            case "JUi":
                return "Jun Ui";
            case "SaUi":
                return "Santō Ui";
            case "NiUi":
                return "Nitō Ui";
            case "ItUi":
                return "Ittō Ui";
            case "SaSa":
                return "Santō Sa";
            case "NiSa":
                return "Nitō Sa";
            case "ItSa":
                return "Ittō Sa";
            default:
                return "Bakuryōchō-taru Onshō";
        }
    }
    
    public static RankType? ToRankType(this string fullName) {
        return Enum.GetValues<RankType>().FirstOrDefault(r => (r).GetFullRank() == fullName);
    }
}