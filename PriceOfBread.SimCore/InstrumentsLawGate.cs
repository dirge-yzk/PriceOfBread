namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (423) 法の門の旧と新の表 lawgate_s（読むだけ・事前登録 measurements/lt_20261019_prereg.txt [2] D7） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsModernize.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末〔modsig_s と同じ選び方〕）。旗 AiLawGateFromBoard＝0 でも出す（A の表が〈もし新しい門なら誰がいつ何を望むか〉の表になる）。
// 列：flagLevel（AiLawGateFromBoard）／goldLawCode（MonetaryLaw の番号）／goldWishOld・goldWishNew（旧と新の金本位の願い・0／1）／goldTargetKept（四半期の末に残る MonetaryTarget）／
//   antiTargetOld・antiTargetNew（反トラストの目標の番号・書かなければ −1）／antiTargetKept（四半期の末の Policy.Antitrust）／antitrustLawCode（現行の法）／
//   guildTargetOld・guildTargetNew（同業組合の目標の番号・書かなければ −1・ゲート 0 も −1）／guildGate（GuildLawGate）／meanIndustrialization／literacyHome（本国の平均識字・兵士を含む）／urbanHome（本国の都市の割合）。
// ⚠限界（計器が自己申告する）：願いの旧と新は四半期の末の盤面で求めた値（実際の DecideQuarter・Apply は四半期の頭）／Kept は四半期の末に残っている目標（政治の経路が書き換えうる）＝願いと施行（法の列）は別／
//   literacyHome は兵士 POP を含む（InstrumentsReform の literacyShare は除く）／挙げた国以外は年末の断面。
public static partial class Instruments
{
    internal const string LawGateHeaderText = "quarter	year	nation	flagLevel	goldLawCode	goldWishOld	goldWishNew	goldTargetKept	antiTargetOld	antiTargetNew	antiTargetKept	antitrustLawCode	guildTargetOld	guildTargetNew	guildGate	meanIndustrialization	literacyHome	urbanHome";

    static void LawGateFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int Code(AntitrustLaw? a) => a is { } v ? (int)v : -1;
        int GCode(GuildLaw? g) => g is { } v ? (int)v : -1;
        sk.LawGate ??= Open("lawgate", seed);
        if (!sk.LawGateHeader) { sk.LawGate.WriteLine(LawGateHeaderText); sk.LawGateHeader = true; }
        sk.LawGate.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), F(w.Coef.AiLawGateFromBoard), I((int)w.MonetaryLaw),
            I(UtilityAgent.GoldWishOld(w) ? 1 : 0), I(UtilityAgent.GoldWishFromBoard(w) ? 1 : 0), I(w.Policy.MonetaryTarget),
            I(Code(UtilityAgent.AntitrustTargetOld(w))), I(Code(UtilityAgent.AntitrustTargetFromBoard(w))), I((int)w.Policy.Antitrust), I((int)w.Antitrust),
            I(GCode(UtilityAgent.GuildTargetOld(w))), I(GCode(UtilityAgent.GuildTargetFromBoard(w))), F(w.Coef.GuildLawGate),
            F(w.MeanIndustrialization), F(PoliticalQuantities.AvgLiteracy(w, coreOnly: true)), F(UtilityAgent.HomeUrbanShare(w))));
    }
}
