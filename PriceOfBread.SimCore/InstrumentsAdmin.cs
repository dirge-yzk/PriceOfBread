namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (408) 行政費の返りの表 admrule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRule.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/ar_20261018_prereg.txt の記録の線 R1〜R3（国庫に届く割合の旧と新・役務が届かない国家区分・法の浸透の旧と新）を読む。取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。旗 AdminServesStateRule＝0 でも出す（A の表が〈旧の世界の届き〉の表になる）。
// 列（単位は接尾辞：Share＝0〜1・Count＝件数。重みは活きた州の人口）：
//   adminFillShare（人口加重の Region.AdminFill）／ruleStateShare・ruleLocalShare（人口加重の Rule[0]・Rule[1]。区分が無い州は 1・0）／
//   deliveryOldShare（旧の式 R0＋R1×d の人口加重・旗に依らない）／deliveryShare（実際の Politics.RuleDelivery の人口加重＝旗に従う）／unservedStateShare（人口加重の R0×(1−AdminFill)）／
//   stateReachOldShare（人口加重の Politics.RuleReach・旗に依らない）／stateReachShare（実際の Politics.StateReach＝旗に従う）／provincesCount（活きた州の数）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（税の月ごとの値は読まない）／人口加重（徴税の重みは所得だが州の中の所得の分布は読まない）／国の平均では州ごとの偏り（植民地の充足 0 など）が隠れる。
public static partial class Instruments
{
    internal const string AdmRuleHeaderText = "quarter\tyear\tnation\tadminFillShare\truleStateShare\truleLocalShare\tdeliveryOldShare\tdeliveryShare\tunservedStateShare\tstateReachOldShare\tstateReachShare\tprovincesCount";

    static void AdmRuleFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        double d = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value);
        double den = 0, fill = 0, r0 = 0, r1 = 0, dOld = 0, dNew = 0, unserved = 0, reachOld = 0; int n = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            bool ok = r.Rule.Length == 4;
            double s0 = ok ? PoliticalQuantities.Clamp01(r.Rule[0]) : 1.0, s1 = ok ? PoliticalQuantities.Clamp01(r.Rule[1]) : 0.0, f = PoliticalQuantities.Clamp01(r.AdminFill);
            den += pop; n++;
            fill += pop * f; r0 += pop * s0; r1 += pop * s1;
            dOld += pop * (s0 + s1 * d); dNew += pop * Politics.RuleDelivery(r, c);
            unserved += pop * (s0 * (1.0 - f)); reachOld += pop * Politics.RuleReach(r);
        }
        if (!(den > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.AdmRule ??= Open("admrule", seed);
        if (!sk.AdmRuleHeader) { sk.AdmRule.WriteLine(AdmRuleHeaderText); sk.AdmRuleHeader = true; }
        sk.AdmRule.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(fill / den), F(r0 / den), F(r1 / den), F(dOld / den), F(dNew / den), F(unserved / den), F(reachOld / den), F(Politics.StateReach(w, c)), I(n)));
    }
}
