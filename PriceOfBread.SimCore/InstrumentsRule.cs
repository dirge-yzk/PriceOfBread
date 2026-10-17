namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (399) 支配の4区分の表 rule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCoup.cs／InstrumentsSecession.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/rs_20261018_prereg.txt の記録の線 R1〜R8（英と清の税÷GDP・分割ポーランドと英の国家区分の順・条約港の代わりの外国区分の州と年・在地の区分が農奴解放の年で消えないこと・
//   法の浸透〔enforce〕の分布・仲介者の取り分・植民地の州の区分・無統治の分布）を読む。取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。
// 行＝国×四半期×活きている州（人口が 0 の国は出さない）。挙げた国は毎四半期・他の全ての国は年末。旗 0 でも出す（A の表は〈開幕のデータと動かない区分〉の表＝B との差が読める）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・K＝千人・Km・Count＝件数・Flag＝0／1・PerMonth＝月あたりの流れ・Code＝法の段の整数）：
//   州の列：tier／popK／ruleStateShare・ruleLocalShare・ruleForeignShare・ruleUngovShare（Region.Rule の 4 区分）／ruleInitLocalShare（開幕のデータの在地・RuleInit[1]）／foreignRulerCount（外国持分の主体の数）／
//     dominanceShare（支配度）／capitalDistanceKm（国の座標からの大圏距離・座標未登録は nan）／reachShare（Politics.RegionReach＝② の到達・旗に依らず計算）。
//   国の列（その国の全ての州の行に同じ値）：serfLawCode・bureaucracyLawCode・landReformLawCode（法の段）／intermediaryDegreeIndex（① 仲介者の権利の残高）／publicOrderIndex（治安）／
//     stateReachIndex（⑤b の enforce の値＝Politics.StateReach・RuleSharesReach=0 なら 1.0）／adminEfficiencyIndex（旧の門の入力＝旗 0 の enforce は min(1, これ÷LawEnforcementAdminGate)）／
//     treatyPortsGrantedCount（旧の個数）・treatyPortInForceFlag（読み替えた述語）・portForeignShare（③ の関税の目減り）／taxLeakPerMonth（直近月の汚職漏れ）／revenuePerMonth・gdpPerMonth（税÷GDP を読み手が組む）／
//     ruleCutPerMonth（直近月の仲介者の取り分＝納税者→州の貴族・World.LastRuleCut・R6）／stateReachHomeIndex（stateReachIndex を植民地格の州を除いた人口加重で読んだ値・R5）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（Instruments.Quarter の呼び時点）／外国持分の主体名は列にしない（個数だけ）／旗 0 の stateReachIndex・reachShare は参考値（本体は読まない）。
public static partial class Instruments
{
    internal const string RuleHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\truleStateShare\truleLocalShare\truleForeignShare\truleUngovShare\truleInitLocalShare\tforeignRulerCount\tdominanceShare\tcapitalDistanceKm\treachShare\t"
        + "serfLawCode\tbureaucracyLawCode\tlandReformLawCode\tintermediaryDegreeIndex\tpublicOrderIndex\tstateReachIndex\tadminEfficiencyIndex\ttreatyPortsGrantedCount\ttreatyPortInForceFlag\tportForeignShare\ttaxLeakPerMonth\trevenuePerMonth\tgdpPerMonth\truleCutPerMonth\tstateReachHomeIndex";

    /// <summary>stateReachIndex の本国の州だけ版（植民地格の州を除く人口加重の <see cref="Politics.RuleReach"/> の平均・読むだけ）。本体の enforce は読まない（<c>StateReach</c> は植民地格の州も含む）。</summary>
    static double HomeStateReach(World w, Coefficients c)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.Tier == RegionTier.Colonial) continue;
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * Politics.RuleReach(r, c); den += pop;
        }
        return den > 0 ? num / den : 1.0;
    }

    static void RuleFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Rule ??= Open("rule", seed);
        if (!sk.RuleHeader) { sk.Rule.WriteLine(RuleHeaderText); sk.RuleHeader = true; }
        string head = I(q) + "\t" + I(w.Year) + "\t" + NameOf(w) + "\t";
        string nat = string.Join('\t', I((int)w.SerfEmancipation), I((int)w.Bureaucracy), I((int)w.LandReform), F(Privileges.IntermediaryDegree(w)), F(w.PublicOrder),
            F(Politics.StateReach(w, c)), F(w.AdminEfficiency), I(w.TreatyPortsGranted), w.UnequalTreatyInForce ? "1" : "0", F(Politics.ForeignPortShare(w)),
            F(w.LastTaxLeak), F(w.LastRevenuePerMonth.Value), F(w.GdpPerMonth.Value), F(w.LastRuleCut), F(HomeStateReach(w, c)));
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            var rc = Places.Coord(r.Name);
            double km = rc.Lat == 0 && rc.Lon == 0 ? double.NaN : Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            bool ok = r.Rule.Length == 4;
            sk.Rule.WriteLine(head + string.Join('\t', r.Name, r.Tier.ToString(), F(r.SumPopulation()),
                F(ok ? r.Rule[0] : double.NaN), F(ok ? r.Rule[1] : double.NaN), F(ok ? r.Rule[2] : double.NaN), F(ok ? r.Rule[3] : double.NaN),
                F(r.RuleInit.Length == 4 ? r.RuleInit[1] : double.NaN), I(r.ForeignRulers.Length), F(r.Dominance), F(km), F(Politics.RegionReach(w, c, r)))
                + "\t" + nat);
        }
    }
}
