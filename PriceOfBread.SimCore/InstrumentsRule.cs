namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (399) 支配の4区分の表 rule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCoup.cs／InstrumentsSecession.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/rs_20261018_prereg.txt の記録の線 R1〜R8（英と清の税÷GDP・分割ポーランドと英の国家区分の順・条約港の代わりの外国区分の州と年・在地の区分が農奴解放の年で消えないこと・
//   法の浸透〔enforce〕の分布・仲介者の取り分・植民地の州の区分・無統治の分布）を読む。取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。
// 行＝国×四半期×活きている州（人口が 0 の国は出さない）。挙げた国は毎四半期・他の全ての国は年末。(399) で区分を点火したので常に動く値を出す（点火前の腕 A〔旗 0〕の表は measurements/rs399_20261018_*）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・K＝千人・Km・Count＝件数・Flag＝0／1・PerMonth＝月あたりの流れ・Code＝法の段の整数）：
//   州の列：tier／popK／ruleStateShare・ruleLocalShare・ruleForeignShare・ruleUngovShare（Region.Rule の 4 区分）／ruleInitLocalShare（開幕のデータの在地・RuleInit[1]）／foreignRulerCount（外国持分の主体の数）／
//     dominanceShare（支配度）／capitalDistanceKm（国の座標からの大圏距離・座標未登録は nan）／reachShare（Politics.RegionReach＝② の到達）。
//   国の列（その国の全ての州の行に同じ値）：serfLawCode・bureaucracyLawCode・landReformLawCode（法の段）／intermediaryDegreeIndex（① 仲介者の権利の残高）／publicOrderIndex（治安）／
//     stateReachIndex（法の浸透 enforce の値＝Politics.StateReach）／adminEfficiencyIndex（行政効率＝(399) で読み手が計器だけになった）／
//     treatyPortInForceFlag（条約港が効いているか）・portForeignShare（③ の関税の目減り）／revenuePerMonth・gdpPerMonth（税÷GDP を読み手が組む）／
//     ruleCutPerMonth（直近月の仲介者の取り分＝納税者→州の貴族・World.LastRuleCut・R6）／stateReachHomeIndex（植民地格の州を除いた人口加重の RuleReach・R5）。
//     ⚠(412) (408) 以後は stateReachIndex（Politics.StateReach）が州ごとに ×AdminFill を掛け、この列は掛けない＝2 列は別の量（1783 の仏で 0.70 と 0.08）。2 列を比べる読み手は (408) 以後の世界では使わない（列の意味を連続に保つため式は変えない）。★(427) 旗 AdminServesStateRule は (419) で焼き込み〔常に 1〕＝〈旗 1 の世界〉は今の世界の全て。
// (842) 段 P1-0＝州ごとの治安の形 P1（下書き docs/202611/20261105/20261105_自給の世帯の家計の段と州ごとの治安_下書き.md §2・AI の案・確証なし）の恒等の計器。末尾の 9 列（nat の後）。挙動は変えない。計器が自分で州の POP と国の Coercion を読む（エンジンの関数の局所変数を受け取らない）。
//   orderRawIndex＝切る前の R＝effCoerce÷max(1e-9, 人口×CrimeOrderPerPop)（Crime.cs と同じ式・四半期の末の w.Coercion で数える。人口は約分される）。
//   militancyRegionIndex＝m_r（州の平民〔IsCommoner〕の人口加重の闘争度・首都を含む・平民が 0 の州は nan）／militancyNationIndex＝m̄（国の全ての州〔首都を含む〕の平民の人口加重の平均・全ての州の行に同じ値）。
//   orderRegionIndex＝order_r＝Clamp01(R×m̄÷m_r)（m_r が正でない州は 1）／ungovTargetOldShare＝(1−Clamp01(w.PublicOrder))×(1−reachShare)＝今のエンジンの無統治の目標／ungovTargetNewShare＝(1−order_r)×(1−reachShare)。
//   遅れた R（下書きの反証役の訂正 8）：w.PublicOrder は Crime.Step が前の四半期の Coercion で書いた値＝1 四半期遅れ。orderRawLagIndex＝前の四半期の末に数えた R（計器の持ち物・最初の四半期は nan）・orderRegionLagIndex・ungovTargetNewLagShare は R の代わりにそれで数えた値。
//     publicOrderIndex が 1 未満の行では orderRawLagIndex と publicOrderIndex が一致する（試験 T7 が固定）。Rule の目標を行ごとに数える州は Rule の長さが 4 の州だけ（それ以外の州の目標の列は nan）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（Instruments.Quarter の呼び時点）／外国持分の主体名は列にしない（個数だけ）／orderRawIndex は四半期の末の Coercion＝エンジンの Rule のループが走った時点の値と同じとは限らない（Rule のループの時点の R を貯めない＝思想⑩）／
//   遅れた R は計器が毎四半期の末に 1 つ持つ値＝挙げた国でない国も毎四半期更新する（年末の行しか出ない国でも遅れは前の四半期の値）。
//   (846) m_r・m̄ とその派生（militancyRegionIndex・militancyNationIndex・orderRegionIndex・ungovTargetNewShare）は四半期の末の値で、エンジンの Rule のループが読む CheckSecession の瞬間の値と 92% の行で違う（(842)・measurements/m842_20261105/rule_vs_secession.txt）＝エンジンの〈働いた〉の数に使わない。
public static partial class Instruments
{
    sealed class RuleLagState { public double Prev = double.NaN; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, RuleLagState> RuleLags = new();

    /// <summary>(842) 切る前の R（計器が自分で書く式）：EffectiveCoerce と同じ結合順 C×(P/ref) を先に、人口は約分されるが約分しない形のまま。人口が正でなければ nan。</summary>
    internal static double RuleRawOrder(World w, Coefficients c)
    {
        double pop = w.TotalPopulation;
        if (!(pop > 0)) return double.NaN;
        double effCoerce = w.Coercion * (pop / System.Math.Max(1.0, c.PoliticalReferencePop));
        return effCoerce / System.Math.Max(1e-9, pop * c.CrimeOrderPerPop);
    }

    /// <summary>(842) 州の平民の闘争度の和と規模の和（IsCommoner の述語を計器が自分で数える）。</summary>
    internal static (double Mil, double Size) RuleCrowdMilitancy(Region r)
    {
        double mil = 0, size = 0;
        foreach (var p in r.Pops) if (p.IsCommoner) { mil += p.Militancy * p.Size; size += p.Size; }
        return (mil, size);
    }

    static double RuleClamp01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;

    internal const string RuleHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\truleStateShare\truleLocalShare\truleForeignShare\truleUngovShare\truleInitLocalShare\tforeignRulerCount\tdominanceShare\tcapitalDistanceKm\treachShare\t"
        + "serfLawCode\tbureaucracyLawCode\tlandReformLawCode\tintermediaryDegreeIndex\tpublicOrderIndex\tstateReachIndex\tadminEfficiencyIndex\ttreatyPortInForceFlag\tportForeignShare\trevenuePerMonth\tgdpPerMonth\truleCutPerMonth\tstateReachHomeIndex"
        + RuleP1HeaderText;

    /// <summary>(842) 段 P1-0 の末尾の 9 列の見出し（先頭にタブ）。</summary>
    internal const string RuleP1HeaderText = "\torderRawIndex\tmilitancyRegionIndex\tmilitancyNationIndex\torderRegionIndex\tungovTargetOldShare\tungovTargetNewShare\torderRawLagIndex\torderRegionLagIndex\tungovTargetNewLagShare";

    /// <summary>本国の州だけの法の到達（植民地格の州を除く人口加重の <see cref="Politics.RuleReach"/> の平均・読むだけ・★(412) 官僚の役務の充足 AdminFill は掛けない＝(408) 以後の stateReachIndex とは別の量）。本体の enforce は読まない（<c>StateReach</c> は植民地格の州も含む）。</summary>
    static double HomeStateReach(World w, Coefficients c)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * Politics.RuleReach(r); den += pop;
        }
        return den > 0 ? num / den : 1.0;
    }

    static void RuleFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        var c = w.Coef;
        // (842) 遅れた R の持ち物：毎四半期（挙げた国でなくても・行を出さない四半期も）更新する。行を出す前に前の値を取り、今の値を次の四半期のために置く。
        var lagSt = RuleLags.GetValue(w, _ => new RuleLagState());
        double rNow = RuleRawOrder(w, c), rLag = lagSt.Prev;
        lagSt.Prev = rNow;
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Rule ??= Open("rule", seed);
        if (!sk.RuleHeader) { sk.Rule.WriteLine(RuleHeaderText); sk.RuleHeader = true; }
        string head = I(q) + "\t" + I(w.Year) + "\t" + NameOf(w) + "\t";
        string nat = string.Join('\t', I((int)w.SerfEmancipation), I((int)w.Bureaucracy), I((int)w.LandReform), F(Privileges.IntermediaryDegree(w)), F(w.PublicOrder),
            F(Politics.StateReach(w, c)), F(w.AdminEfficiency), w.UnequalTreatyInForce ? "1" : "0", F(Politics.ForeignPortShare(w)),
            F(w.LastRevenuePerMonth.Value), F(w.GdpPerMonth.Value), F(w.LastRuleCut), F(HomeStateReach(w, c)));
        // (842) 段 P1-0：m̄＝国の全ての州（首都を含む）の平民の人口加重の平均（州の列と別に 1 回数える）
        double crowdMil = 0, crowdSize = 0;
        foreach (var r in w.Regions) { var (cm, cs) = RuleCrowdMilitancy(r); crowdMil += cm; crowdSize += cs; }
        double mBar = crowdSize > 1e-9 ? crowdMil / crowdSize : double.NaN;
        double orderOld = RuleClamp01(w.PublicOrder);
        foreach (var r in w.Regions)
        {
            var rc = Places.Coord(r.Name);
            double km = rc.Lat == 0 && rc.Lon == 0 ? double.NaN : Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            bool ok = r.Rule.Length == 4;
            double reach = Politics.RegionReach(w, c, r);
            var (rmil, rsize) = RuleCrowdMilitancy(r);
            double mReg = rsize > 1e-9 ? rmil / rsize : double.NaN;
            double orderNew = !(mReg > 0) ? 1.0 : RuleClamp01(rNow * mBar / mReg);
            double orderLag = double.IsNaN(rLag) ? double.NaN : !(mReg > 0) ? 1.0 : RuleClamp01(rLag * mBar / mReg);
            string p1 = string.Join('\t', F(rNow), F(mReg), F(mBar), F(orderNew),
                F(ok ? (1.0 - orderOld) * (1.0 - reach) : double.NaN), F(ok ? (1.0 - orderNew) * (1.0 - reach) : double.NaN),
                F(rLag), F(orderLag), F(ok ? (1.0 - orderLag) * (1.0 - reach) : double.NaN));
            sk.Rule.WriteLine(head + string.Join('\t', r.Name, r.Tier.ToString(), F(r.SumPopulation()),
                F(ok ? r.Rule[0] : double.NaN), F(ok ? r.Rule[1] : double.NaN), F(ok ? r.Rule[2] : double.NaN), F(ok ? r.Rule[3] : double.NaN),
                F(r.RuleInit.Length == 4 ? r.RuleInit[1] : double.NaN), I(r.ForeignRulers.Length), F(r.Dominance), F(km), F(reach))
                + "\t" + nat + "\t" + p1);
        }
    }
}
