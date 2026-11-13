namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (408) 行政費の返りの表 admrule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRule.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/ar_20261018_prereg.txt の記録の線 R1〜R3（国庫に届く割合の旧と新・役務が届かない国家区分・法の浸透の旧と新）を読む。取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。（旗 AdminServesStateRule は (419) で焼き込み〔常に 1〕。旧の式の列〔deliveryOld・stateReachOld〕は旧との差を読む比較の物差しとして残す。）
// 列（単位は接尾辞：Share＝0〜1・Count＝件数。重みは活きた州の人口）：
//   adminFillShare（人口加重の Region.AdminFill）／ruleStateShare・ruleLocalShare（人口加重の Rule[0]・Rule[1]。区分が無い州は 1・0）／
//   deliveryOldShare（旧の式 R0＋R1×d の人口加重・比較の物差し）／deliveryShare（実際の Politics.RuleDelivery の人口加重）／unservedStateShare（人口加重の R0×(1−AdminFill)）／
//   stateReachOldShare（人口加重の Politics.RuleReach・比較の物差し）／stateReachShare（実際の Politics.StateReach）／provincesCount（活きた州の数）。
//   ★追記2（2026-10-18）で足した列（既存の列の後ろ・K＝千人）：ruleCutRateAllShare・ruleCutRateLordedShare（人口加重の仲介者の取り分の率・後者は領主が居る州だけ＝差が納税者の手元に残る減税）／lordlessPopShare（領主の居ない州の人口割合）／
//   manLimitK・manLimitOldK（人口の側の定員 MaxSoldierFraction×兵役の母数の実際と旧の届き方の値）／seatsK（EstablishmentOf の定員）／popSideDecidesFlag（定員が人口の側で決まるか）／
//   soldierK・excessSoldierK（兵士の数と、平時の復員の対象〔Meritocracy ⑦ と同じ州按分で席を超えた分〕）／atWarFlag／nobleIncomeSumPerMonth・nobleIncomeMaxPerCapPerMonth（領主〔貴族 POP〕の前月の所得の和と一人あたりの最大）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（税の月ごとの値は読まない）／人口加重（徴税の重みは所得だが州の中の所得の分布は読まない）／国の平均では州ごとの偏り（植民地の充足 0 など）が隠れる。
public static partial class Instruments
{
    internal const string AdmRuleHeaderText = "quarter\tyear\tnation\tadminFillShare\truleStateShare\truleLocalShare\tdeliveryOldShare\tdeliveryShare\tunservedStateShare\tstateReachOldShare\tstateReachShare\tprovincesCount\truleCutRateAllShare\truleCutRateLordedShare\tlordlessPopShare\tmanLimitK\tmanLimitOldK\tseatsK\tpopSideDecidesFlag\tsoldierK\texcessSoldierK\tatWarFlag\tnobleIncomeSumPerMonth\tnobleIncomeMaxPerCapPerMonth";

    static void AdmRuleFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        double d = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value);
        double den = 0, fill = 0, r0 = 0, r1 = 0, dOld = 0, dNew = 0, unserved = 0, reachOld = 0; int n = 0;
        double cutAll = 0, cutLorded = 0, lordless = 0, manOld = 0, nobleSum = 0, nobleMaxPerCap = 0;
        foreach (var r in w.Regions)
        {
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            bool ok = r.Rule.Length == 4;
            double s0 = ok ? PoliticalQuantities.Clamp01(r.Rule[0]) : 1.0, s1 = ok ? PoliticalQuantities.Clamp01(r.Rule[1]) : 0.0, f = PoliticalQuantities.Clamp01(r.AdminFill);
            den += pop; n++;
            fill += pop * f; r0 += pop * s0; r1 += pop * s1;
            dOld += pop * (s0 + s1 * d); dNew += pop * Politics.RuleDelivery(r, c);
            unserved += pop * (s0 * (1.0 - f)); reachOld += pop * Politics.RuleReach(r);
            // 仲介者の取り分の率（Fiscal.TaxScan.Of の cut と同じ式の読み）：領主が居る州だけが領主へ移し、居ない州は納税者の手元に残る（＝減税として働く）
            bool hasLord = false;
            foreach (var lp in r.Pops)
                if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9)
                {
                    hasLord = true;
                    double inc = Math.Max(0, lp.LastEarnedPerMonth.Value); nobleSum += inc; nobleMaxPerCap = Math.Max(nobleMaxPerCap, inc / lp.Size);
                }
            double cutRate = ok ? (s1 + s0 * (1.0 - f)) * (1.0 - d) : 0.0;
            cutAll += pop * cutRate; if (hasLord) cutLorded += pop * cutRate; else lordless += pop;
            // 徴兵の母数（RegionManpower＝RuleDelivery を共有）：実際の値と、旧の届き方（R0＋R1×d）に置き換えた値（旧＝実際÷実際の届け率×旧の届け率・届け率 0 の州は母数も 0）
            double mNow = Military.RegionManpower(r, c), dNow = Politics.RuleDelivery(r, c);
            manOld += dNow > 0 ? mNow / dNow * (s0 + s1 * d) : 0.0;
        }
        if (!(den > 0)) return;
        // 定員（EstablishmentOf＝min(経済の側, 人口の側)）と平時の復員の対象（Meritocracy ⑦ と同じ州按分で、兵士 POP が席を超えた分）
        double seats = Military.EstablishmentOf(w, c, out double natMan), soldier = 0, excess = 0;
        double manLimit = c.MaxSoldierFraction.Value * natMan;
        foreach (var r in w.Regions)
        {
            Pop? sp = null;
            foreach (var p in r.Pops) if (p.IsSoldier) { sp = p; break; }
            if (sp is null) continue;
            soldier += sp.Size;
            if (natMan > 1e-9) excess += Math.Max(0, sp.Size - seats * (Military.RegionManpower(r, c) / natMan));
        }
        int popSide = seats > 0 && Math.Abs(seats - manLimit) <= 1e-9 * Math.Max(1.0, manLimit) ? 1 : 0;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.AdmRule ??= Open("admrule", seed);
        if (!sk.AdmRuleHeader) { sk.AdmRule.WriteLine(AdmRuleHeaderText); sk.AdmRuleHeader = true; }
        sk.AdmRule.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(fill / den), F(r0 / den), F(r1 / den), F(dOld / den), F(dNew / den), F(unserved / den), F(reachOld / den), F(Politics.StateReach(w, c)), I(n),
            F(cutAll / den), F(cutLorded / den), F(lordless / den), F(manLimit), F(c.MaxSoldierFraction.Value * manOld), F(seats), I(popSide), F(soldier), F(excess), I(w.AtWarNow ? 1 : 0), F(nobleSum), F(nobleMaxPerCap)));
    }
}
