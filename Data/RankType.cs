namespace SMASSB.Data;

public enum RankType {
    Kō = 15,
    NiShi = 16,
    ItShi = 250,
    Shi = 500,
    SaSō = 750,
    NiSō = 1000,
    ItSō = 1250,
    Sō = 1500,
    JUi = 50000,
    SaUi = 50001,
    NiUi = 50002,
    ItUi = 50003,
    SaSa = 50004,
    NiSa = 50005,
    ItSa = 50006,
    Onshō = 50007
}

public static class RankTypeMethods {

    public static string GetFullRank(this Enum rank) {
        
        var rankInfo = rank.GetType().GetField(rank.ToString());

        switch (rankInfo?.Name) {
            case "Kō":
                return "Jieikan Kōhosei";
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
    
    public static (ulong rank, ulong category) GetRoleId(this string fullName) {

        switch (fullName) {
            case "Jieikan Kōhosei":
                return (1475886792174604484, 1473369036766052445);
            case "Nitō Shi":
                return (1475886748268625962, 1473368797023961139);
            case "Ittō Shi":
                return (1475886729561899212, 1473368797023961139);
            case "Shichō":
                return (1475886715368509753, 1473368797023961139);
            case "Santō Sō":
                return (1475886697118957660, 1473368797023961139);
            case "Nitō Sō":
                return (1475886671919579310, 1473368797023961139);
            case "Ittō Sō":
                return (1475886657545961472, 1473368797023961139);
            case "Sōchō":
                return (1475886640429011125, 1473368797023961139);
            case "Jun Ui":
                return (1475886599756841062, 1473368699523305708);
            case "Santō Ui":
                return (1475886578382541036, 1473368699523305708);
            case "Nitō Ui":
                return (1475886521646321825, 1473368699523305708);
            case "Ittō Ui":
                return (1475886499856777399, 1473368699523305708);
            case "Santō Sa":
                return (1475886479250166013, 1473368340314460170);
            case "Nitō Sa":
                return (1475886462410166393, 1473368340314460170);
            case "Ittō Sa":
                return (1475886441484779561, 1473368340314460170);
            default:
                return (1475886395276005497, 1473368268239798396);
        }
    }
}