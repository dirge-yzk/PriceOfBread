using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (837) 自給の世帯の家計の段 1 の恒等の計器 subst_s（読むだけ・恒等・事前登録 measurements/m837_20261104_prereg_ss.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCearn.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。INS=1 のときだけ動く（Instruments.Quarter から呼ばれる）。
// 目的＝S3 の段 1（c65ds 下書き 2-2）：〈売れ残った農業労働×導いた生産性〉で作る自給の量と、今のエンジンの自給の量（GoodNeed×Subsistence×収穫係数）を、同じ盤面で計器が両方数える。
//   売れ残りの基準は 2 つ並べる（D1＝能力の基準・D2＝産出の基準＝今の失業の式の分母）。基準を決めるのは設計の材料の線（c67ds）＝この表は数だけを出す。
// エンジンの関数を呼ばない：農業労働の売り手の判定・人頭税の請求・地代の按分は計器が自分で書く。エンジンの状態は欄（市場の LastSold・LastProduced・生産者の Capacity・POP の Size・Subsistence・LandRentPerMonth・Region.LastSubsistenceOutput）として読むだけ。
// 行＝担い手〔自給の担い手の人数＞0〕か農業労働の売り手がいる州×四半期（全ての国）。額は州の持ち主の国の通貨・数量は財の単位・どれも直近の月の値（…PerMonth）。
// ⚠限界（計器が自己申告する）：①四半期の末＝直近の月の値②nominalSubPerMonth は収穫係数 1・補正なしの名目（engineSubOutputPerMonth は収穫係数・動物性の補正・財の門を含む）③農業労働の売り手が複数のとき担い手の POP は最初の売り手のものを指す（売れた量は市場の 1 本）
//   ④人頭税は請求の上界（届く割合と払える割合を掛けない）・所得税は身分の免税と課税への同意を掛けない上界⑤rentToOthersPerMonth は地代を受け取る他の層の月の和（輸出の地代 PaySales は LandRentPerMonth に載らない＝入らない）⑥Subsistence の担い手は農民以外も数える（エンジンと同じ読み方）。
public static partial class Instruments
{
    /// <summary>計器の名乗りの定数（係数ではない）：開幕の自給の生産性の較正＝0.7×1.10÷0.866 ≈ 0.889 を 2 桁に丸めた値（c66ds が (825) の 1.1 を取り直した値・1 人あたり月）。</summary>
    internal const double SubstCalibration = 0.89;
    /// <summary>計器の名乗りの定数（係数ではない）：開幕 1783〜85 の売れ残りの能力の基準 D1 の物差し（c66ds 材料 D-2 の 0.866）。国のデータから生産性を導く式の分母。</summary>
    internal const double SubstD1Ref = 0.866;

    internal const string SubstHeaderText = "quarter\tyear\tnation\tregion\ttier\tsubPopK\tsubRateWeighted\t"
        + "farmCapPerMonth\tfarmSoldPerMonth\tfarmProducedPerMonth\tsoldOverCap\tunsoldD1Share\tunsoldD2Share\tproducedZeroFlag\t"
        + "stapleNeedPerCapita\tengineSubOutputPerMonth\tnominalSubPerMonth\tproductivityFromDataPerCapita\t"
        + "derivedD1PerMonth\tderivedD2PerMonth\tderivedD1OverNominal\tderivedD2OverNominal\tderivedD1OverEngine\tderivedD2OverEngine\t"
        + "pollClaimPerMonth\tincomeTaxUpperPerMonth\trentToOthersPerMonth\trentToFarmersPerMonth\t"
        + "stapleNeedWeightedPrice\tstapleNeedValuePerMonth\tmarketShareOfNeed\tobligationOverMarketNeed\t"
        + SubstS2HeaderText;

    sealed class SubstSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SubstSink> SubstSinks = new();

    /// <summary>D1（純関数）＝能力の基準の売れ残り＝clamp01(1−売れた÷能力)。能力が 0 以下なら nan。</summary>
    internal static double SubstD1(double sold, double cap) => cap > 0 ? System.Math.Clamp(1.0 - sold / cap, 0.0, 1.0) : double.NaN;

    /// <summary>D2（純関数）＝産出の基準の売れ残り＝clamp01(1−売れた÷産出)。産出が 1e-9 以下なら 0（今の失業の式の Eps の枝と同じ）。</summary>
    internal static double SubstD2(double sold, double produced) => produced > 1e-9 ? System.Math.Clamp(1.0 - sold / produced, 0.0, 1.0) : 0.0;

    /// <summary>導いた自給の量（純関数）＝売れ残りの割合×生産性（1 人あたり月）×担い手の人数。</summary>
    internal static double SubstDerived(double unsoldShare, double productivityPerCapita, double subPop) => unsoldShare * productivityPerCapita * subPop;

    /// <summary>割り算（純関数）：分母が 0 以下なら nan。</summary>
    static double SubstDiv(double a, double b) => b > 0 ? a / b : double.NaN;

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の全ての州の行を書く。</summary>
    static void SubstTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("subst_s")) return;   // (1061) 選ばれない表は組み立ても飛ばす
        var sk = SubstSinks.GetValue(sim, _ => new SubstSink());
        sk.W ??= Open("subst", seed);
        if (!sk.Header) { sk.W.WriteLine(SubstHeaderText); sk.Header = true; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // 主食の名目の必要量（Catalog の BaseNeed の和）は試合の中で変わらない＝1 回だけ数える
        double stapleNeed = 0;
        foreach (var gd in Catalog.Goods) if (gd.Tier == GoodTier.Staple) stapleNeed += gd.BaseNeed;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = NameOf(w);
            var c = w.Coef;
            double pollLevel = w.PollTax, taxRate = w.TaxRate.Value;
            foreach (var r in w.Regions)
            {
                // 農業労働の売り手（人力）：財＝農業労働力・レシピ名が空・投入が空・倒産でない。能力は Capacity＞0 の和（labor_s の LwGood と同じ読み方・エンジンの関数は呼ばない）
                int lp = -1; double cap = 0;
                foreach (var s in r.Producers)
                {
                    if (s.GoodId != Catalog.FarmLaborId || s.RecipeName.Length != 0 || s.Inputs.Count != 0 || s.Bankrupt) continue;
                    if (lp < 0) lp = s.LaborPopIndex;
                    if (s.Capacity > 0) cap += s.Capacity;
                }
                double sold = 0, produced = 0;
                if (lp >= 0 && Catalog.FarmLaborId < r.Markets.Length) { var fm = r.Markets[Catalog.FarmLaborId]; sold = fm.LastSold; produced = fm.LastProduced; }
                // 自給の担い手（Subsistence＞0）とそれ以外の POP
                double subPop = 0, subWeighted = 0, poll = 0, incTax = 0, rentOthers = 0, rentFarmers = 0;
                foreach (var p in r.Pops)
                {
                    double rent = System.Math.Max(0, p.LandRentPerMonth.Value);
                    if (p.SubsistenceBasis > 0)   // ★(849) 担い手＝自給の基が正（旗 0 では旧の Subsistence＞0 と同じ集合）・割合は焼いた POP だけこの月の食べる割合 SubsistenceShare・他は欄（その四半期に Run を通らない州の POP の食べる割合の欄は 0＝cpop_s と同じ三項式）
                    {
                        subPop += p.Size; subWeighted += p.Size * (p.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? p.SubsistenceShare : p.SubsistenceBasis); rentFarmers += rent;
                        if (pollLevel > 1e-9 && !p.Closed && !p.IsSoldier && p.ClassId != PopClass.Criminal) poll += c.PollTaxRate * pollLevel * p.Size;
                        incTax += taxRate * System.Math.Max(0, p.LastEarnedPerMonth.Value);
                    }
                    else rentOthers += rent;
                }
                if (!(subPop > 0) && lp < 0) continue;
                double subRate = subPop > 0 ? subWeighted / subPop : 0.0;
                double d1 = SubstD1(sold, cap), d2 = SubstD2(sold, produced);
                double nominal = subWeighted * stapleNeed;   // Σ Size×Subsistence×主食の名目の必要量（収穫係数 1・補正なし）
                double prodFromData = subRate * stapleNeed / SubstD1Ref;
                double dd1 = SubstDerived(d1, SubstCalibration, subPop), dd2 = SubstDerived(d2, SubstCalibration, subPop);
                // 主食の必要量で重み付けた州の値段
                double pw = 0, pn = 0; int ng = System.Math.Min(r.Markets.Length, Catalog.Goods.Length);
                for (int g = 0; g < ng; g++)
                    if (Catalog.Goods[g].Tier == GoodTier.Staple) { pw += Catalog.Goods[g].BaseNeed * r.Markets[g].Price; pn += Catalog.Goods[g].BaseNeed; }
                double stapleP = pn > 0 ? pw / pn : double.NaN;
                double needValue = subPop * stapleNeed * stapleP;
                double marketShare = 1.0 - subRate;
                double engineSub = r.LastSubsistenceOutput;
                var cols = new List<string>(40)
                {
                    I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), F(subPop), F(subRate),
                    F(cap), F(sold), F(produced), F(cap > 0 ? sold / cap : double.NaN), F(d1), F(d2), I(produced <= 1e-9 ? 1 : 0),
                    F(stapleNeed), F(engineSub), F(nominal), F(prodFromData),
                    F(dd1), F(dd2), F(SubstDiv(dd1, nominal)), F(SubstDiv(dd2, nominal)), F(SubstDiv(dd1, engineSub)), F(SubstDiv(dd2, engineSub)),
                    F(poll), F(incTax), F(rentOthers), F(rentFarmers),
                    F(stapleP), F(needValue), F(marketShare), F(SubstDiv(poll + incTax + rentOthers, marketShare * needValue)),
                };
                SubstS2Columns(r, c, lp, cols);   // ★(849) 末尾の列（自給の段 2 の計器）
                sk.W.WriteLine(string.Join('\t', cols));
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void SubstClose(MultiNationSim sim)
    {
        if (SubstSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }

    // ---------------------------------------------------------------- (849) 自給の段 2 の計器（subst_s の末尾の列・読むだけ・事前登録 measurements/m849_20261105_prereg_ss.txt） ----------------------------------------------------------------
    // 計器はエンジンの関数を呼ばない：人力の農業労働の生産者・売れた量・主食の RGO の効率化材の倍率・ρ の焼き方は計器が自分で書く（事前登録 [1] の文から）。エンジンの状態は欄として読むだけ。
    // 月の頭（EconomyStep.Run の頭・エンジンの頭 SubsistenceHead の前）に SubstMonthHead が州ごとの材料を撮り、四半期の末に SubstS2Columns が列を書く。
    // 計器の ρ は、その州を計器が最初に見た月の頭（開幕の州は助走の後の最初の月＝エンジンが焼く月）に、担い手の欄の割合 d から計器が自分で焼く（旗 0 でも数える＝反実仮想）。
    //   その時に担い手の欄がもう ρ（植民地の写し）なら計器の ρ はエンジンの欄を写す（s2RhoSource＝2・d は nan）。
    // ⚠限界：①σ の計器と エンジンは四半期の末の月（3 か月目）だけを並べる②失業 (b′) の人数は人力の最初の生産者の HeadsLastMonth（能力の式の頭数＝労働財は fraction 1）
    //   ③焼いた月の点検（s2BakeSigmaDiff・s2BakeRhoRelDiff）は次の月の頭で読む（エンジンの食べる割合はその時まで欄に残る）＝焼いた後の行では同じ値を書き続ける。
    internal const string SubstS2HeaderText = "s2BasisKind\ts2BasisEngine\ts2D0Inst\ts2RhoInst\ts2RhoSource\ts2SigmaEngine\ts2SigmaInst\t"
        + "s2CapHead\ts2SoldHead\ts2UHead\ts2TechHead\ts2Tech0Inst\ts2EffFillHead\ts2EffFill0Inst\ts2BakeSigmaDiff\ts2BakeRhoRelDiff\t"
        + "s2CapEnd\ts2SoldEnd\ts2UEnd\ts2TechEnd\ts2HeadsEnd\ts2LneedEnd\ts2EEnd\ts2UnempEngine\ts2UnempB\ts2NewOverOld";

    sealed class SubstHeadState
    {
        public bool Seen, BakePending; public int RhoSource;
        public double D0 = double.NaN, Rho = double.NaN, Tech0 = double.NaN, Eff0 = double.NaN;
        public double SigmaInst = double.NaN, CapHead = double.NaN, SoldHead = double.NaN, UHead = double.NaN, TechHead = double.NaN, EffHead = double.NaN;
        public double BakeSigmaDiff = double.NaN, BakeRhoRelDiff = double.NaN;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, SubstHeadState> SubstHeads = new();

    /// <summary>計器の読み（純関数ではないが書かない）：人力の農業労働の生産者の能力の和・最初の担い手・最初の生産者の頭数。</summary>
    static (double cap, int lp, double heads) S2FarmLabor(Region r)
    {
        int lp = -1; double cap = 0, heads = double.NaN;
        foreach (var s in r.Producers)
        {
            if (s.GoodId != Catalog.FarmLaborId || s.RecipeName.Length != 0 || s.Inputs.Count != 0 || s.Bankrupt) continue;
            if (lp < 0) { lp = s.LaborPopIndex; heads = s.HeadsLastMonth; }
            if (s.Capacity > 0) cap += s.Capacity;
        }
        return (cap, lp, heads);
    }

    /// <summary>計器の読み：主食の RGO（IsRgo・主食・倒産でない・産出＞0）の産出で重みづけた (1＋EffBoost×EffFill) と EffFill（Σ 産出≦0 なら 1 と 0）。</summary>
    static (double tech, double eff) S2Tech(Region r, Coefficients c)
    {
        double w = 0, wt = 0, we = 0;
        foreach (var s in r.Producers)
        {
            if (!s.IsRgo || s.Bankrupt || s.GoodId >= Catalog.Goods.Length || Catalog.Goods[s.GoodId].Tier != GoodTier.Staple || !(s.Output > 0)) continue;
            w += s.Output; wt += s.Output * (1.0 + c.EffBoost * s.EffFill); we += s.Output * s.EffFill;
        }
        return w > 0 ? (wt / w, we / w) : (1.0, 0.0);
    }

    static double S2StapleNeed() { double n = 0; foreach (var gd in Catalog.Goods) if (gd.Tier == GoodTier.Staple) n += gd.BaseNeed; return n; }
    static double S2Unsold(double sold, double cap) => cap > 0 ? cap * System.Math.Clamp(1.0 - sold / cap, 0.0, 1.0) : 0.0;

    /// <summary>月の頭（EconomyStep.Run の頭・エンジンの SubsistenceHead の前・助走の月は呼ばれない）：州の材料を撮る。前の月に焼いた州はエンジンの焼いた結果を点検する。</summary>
    internal static void SubstMonthHead(World w, Region r)
    {
        if (!Wants("subst_s")) return;   // (1061) SubstHeads の読み手は SubstS2Columns（subst_s の行）だけ
        var h = SubstHeads.GetValue(r, _ => new SubstHeadState());
        var (cap, lp, _) = S2FarmLabor(r);
        double sold = Catalog.FarmLaborId < r.Markets.Length ? r.Markets[Catalog.FarmLaborId].LastSold : 0;
        double u = S2Unsold(sold, cap);
        var (t, eff) = S2Tech(r, w.Coef);
        double need = S2StapleNeed();
        Pop? pp = lp >= 0 && lp < r.Pops.Count ? r.Pops[lp] : null;
        if (h.BakePending && pp is not null)
        {
            h.BakeSigmaDiff = System.Math.Abs(pp.SubsistenceShare - h.D0);   // 欄に残っているのは前の月（焼いた月）の食べる割合
            h.BakeRhoRelDiff = pp.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? System.Math.Abs(pp.SubsistenceBasis / h.Rho - 1.0) : double.NaN;
            h.BakePending = false;
        }
        if (!h.Seen && pp is not null)
        {
            h.Seen = true;
            if (pp.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) { h.Rho = pp.SubsistenceBasis; h.RhoSource = 2; }
            else if (pp.SubsistenceBasis > 0 && u > 0 && pp.Size > 0)
            {
                h.D0 = pp.SubsistenceBasis; h.Rho = h.D0 * need * pp.Size / (u * t); h.Tech0 = t; h.Eff0 = eff; h.RhoSource = 1; h.BakePending = true;
            }
        }
        h.CapHead = cap; h.SoldHead = sold; h.UHead = u; h.TechHead = t; h.EffHead = eff;
        h.SigmaInst = pp is not null && h.Rho == h.Rho ? (pp.Size > 0 ? System.Math.Min(1.0, h.Rho * u * t / (need * pp.Size)) : 0.0) : double.NaN;
    }

    /// <summary>四半期の末（<see cref="SubstTable"/> の行の末尾）：自給の段 2 の列。lp＝その行の人力の農業労働の最初の担い手（無ければ −1）。</summary>
    static void SubstS2Columns(Region r, Coefficients c, int lp, List<string> cols)
    {
        Pop? pp = lp >= 0 && lp < r.Pops.Count ? r.Pops[lp] : null;
        SubstHeads.TryGetValue(r, out var h);
        var (capE, _, heads) = S2FarmLabor(r);
        double soldE = Catalog.FarmLaborId < r.Markets.Length ? r.Markets[Catalog.FarmLaborId].LastSold : 0;
        double uE = S2Unsold(soldE, capE);
        var (tE, _) = S2Tech(r, c);
        double need = S2StapleNeed();
        double rho = h?.Rho ?? double.NaN;
        double lneed = need * heads / (rho * tE);
        double e = uE - lneed;
        double ub = rho == rho ? (capE > 0 ? System.Math.Clamp(e / capE, 0.0, 1.0) : 0.0) : double.NaN;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        double nan = double.NaN;
        cols.Add(pp is null ? "-1" : I((int)pp.SubsistenceBasisKind)); cols.Add(F(pp?.SubsistenceBasis ?? nan));
        cols.Add(F(h?.D0 ?? nan)); cols.Add(F(rho)); cols.Add(I(h?.RhoSource ?? 0));
        cols.Add(F(pp?.SubsistenceShare ?? nan)); cols.Add(F(h?.SigmaInst ?? nan));
        cols.Add(F(h?.CapHead ?? nan)); cols.Add(F(h?.SoldHead ?? nan)); cols.Add(F(h?.UHead ?? nan)); cols.Add(F(h?.TechHead ?? nan)); cols.Add(F(h?.Tech0 ?? nan));
        cols.Add(F(h?.EffHead ?? nan)); cols.Add(F(h?.Eff0 ?? nan)); cols.Add(F(h?.BakeSigmaDiff ?? nan)); cols.Add(F(h?.BakeRhoRelDiff ?? nan));
        cols.Add(F(capE)); cols.Add(F(soldE)); cols.Add(F(uE)); cols.Add(F(tE)); cols.Add(F(heads)); cols.Add(F(lneed)); cols.Add(F(e));
        cols.Add(F(pp?.UnemploymentRate.Value ?? nan)); cols.Add(F(ub)); cols.Add(F(pp is not null && h is not null ? pp.SubsistenceShare / h.D0 : nan));
    }
}
