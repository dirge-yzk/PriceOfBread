namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsLawGate.cs#1〕
public static partial class Instruments
{
    internal const string LawGateHeaderText = "quarter	year	nation	goldLawCode	goldWishNew	goldTargetKept	antiTargetNew	antiTargetKept	antitrustLawCode	guildTargetNew	guildGate	meanIndustrialization	literacyHome	urbanHome";

    static void LawGateFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int Code(AntitrustLaw? a) => a is { } v ? (int)v : -1;
        int GCode(GuildLaw? g) => g is { } v ? (int)v : -1;
        sk.LawGate ??= Open("lawgate", seed);
        if (!sk.LawGateHeader) { sk.LawGate.WriteLine(LawGateHeaderText); sk.LawGateHeader = true; }
        sk.LawGate.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), I((int)w.MonetaryLaw),
            I(UtilityAgent.GoldWishFromBoard(w) ? 1 : 0), I(w.Policy.MonetaryTarget),
            I(Code(UtilityAgent.AntitrustTargetFromBoard(w))), I((int)w.Policy.Antitrust), I((int)w.Antitrust),
            I(GCode(UtilityAgent.GuildTargetFromBoard(w))), F(w.Coef.GuildLawGate),
            F(w.MeanIndustrialization), F(PoliticalQuantities.AvgLiteracy(w, coreOnly: true)), F(UtilityAgent.HomeUrbanShare(w))));
    }
}
