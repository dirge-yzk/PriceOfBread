using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。〔履歴 InstrumentsSubst.cs#1〕
public static partial class Instruments
{
    /// <summary>
    /// 〔履歴 InstrumentsSubst.cs#2〕
    /// </summary>
    internal const double SubstCalibration = 0.89;
    /// <summary>
    /// 国のデータから生産性を導く式の分母。〔履歴 InstrumentsSubst.cs#3〕
    /// </summary>
    internal const double SubstD1Ref = 0.866;

    internal const string SubstHeaderText = "quarter\tyear\tnation\tregion\ttier\tsubPopK\tsubRateWeighted\t"
        + "farmCapPerMonth\tfarmSoldPerMonth\tfarmProducedPerMonth\tsoldOverCap\tunsoldD1Share\tunsoldD2Share\tproducedZeroFlag\t"
        + "stapleNeedPerCapita\tengineSubOutputPerMonth\tnominalSubPerMonth\tproductivityFromDataPerCapita\t"
        + "derivedD1PerMonth\tderivedD2PerMonth\tderivedD1OverNominal\tderivedD2OverNominal\tderivedD1OverEngine\tderivedD2OverEngine\t"
        + "pollClaimPerMonth\tincomeTaxUpperPerMonth\trentToOthersPerMonth\trentToFarmersPerMonth\t"
        + "stapleNeedWeightedPrice\tstapleNeedValuePerMonth\tmarketShareOfNeed\tobligationOverMarketNeed\t"
        + SubstS2HeaderText + "\t" + SubstS3HeaderText;

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
        if (!Tg.Subst) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!Wants("subst_s")) return;   // subst_s〔履歴 InstrumentsSubst.cs#4〕
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
                    if (p.SubsistenceBasis > 0)   // SubsistenceBasis〔履歴 InstrumentsSubst.cs#5〕
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
                SubstS2Columns(r, c, lp, cols);   // SubstS2Columns〔履歴 InstrumentsSubst.cs#6〕
                SubstS3Columns(w, r, c, lp, cols);   // (1317) 段 3-0 の器＝s3 の列（読むだけ）
                sk.W.WriteLine(string.Join('\t', cols));
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void SubstClose(MultiNationSim sim)
    {
        if (!Tg.Subst) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (SubstSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }

    // エンジンの状態は欄として読むだけ。〔履歴 InstrumentsSubst.cs#7〕
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
        if (!Tg.Subst) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!Wants("subst_s")) return;   // subst_s〔履歴 InstrumentsSubst.cs#8〕
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

    // ---- (1317) 自給の世帯の家計の段 3-0 の器：subst_s の末尾の s3 の列（読むだけ・エンジンの関数を呼ばず式を写す・新しい状態なし）----
    internal const string SubstS3HeaderText = "s3SrawInst\ts3HarvestInst\ts3QInst\ts3DInst\ts3EInst\ts3EWheatInst\ts3ESoldShareInst\ts3EValueInst\ts3KappaInst\ts3KappaEInst\t"
        + "s3UnempNewInst\ts3EatPerCapInst\ts3LandTaxPeasantInst";

    /// <summary>主食 1 種の材料（純関数への入力）：名目の必要量・収穫係数 f・売る財の売れ行き sold÷(sold＋在庫)・売る財の値段・売る財の量への換算の積・パンのように遡る財か。</summary>
    internal readonly record struct S3Staple(double BaseNeed, double F, double SellShare, double PriceR, double QtyToR, bool Chain);

    /// <summary>s3 の数の列（純関数）。</summary>
    internal readonly record struct S3Row(double Sraw, double Harvest, double Q, double D, double E, double EWheat, double ESoldShare, double EValue, double Kappa, double KappaE,
        double UnempNew, double EatPerCap);

    /// <summary>
    /// 段 3-0 の定義（純関数）：σraw＝ρ×U×T÷(必要量×人数)・x＝σraw×f・D＝GoodNeed×min(1,x)（σraw≦1 かつ f＝1 の時だけ今の sub と同じ）・Q＝GoodNeed×x・E＝GoodNeed×max(0,x−1)。
    /// Lneed_f＝必要量×人数÷(ρ×T×f̄)・s_E＝ΣE×q×売れ行き÷ΣE×q（ΣE＝0 なら 0）。rho が nan なら全て nan。技術の括弧（案 C の影）の列は技術水準の読み手の番人（TechLevelRemovalTests）と衝突するため 3-2 の線へ回した。
    /// </summary>
    internal static S3Row SubstS3Calc(double rho, double u, double t, double cap, double size, System.ReadOnlySpan<S3Staple> st)
    {
        double nan = double.NaN;
        if (!(rho == rho)) return new S3Row(nan, nan, nan, nan, nan, nan, nan, nan, nan, nan, nan, nan);
        double need = 0, fw = 0;
        foreach (var s in st) { need += s.BaseNeed; fw += s.BaseNeed * s.F; }
        double fbar = need > 0 ? fw / need : nan;
        double sraw = size > 0 && need > 0 ? rho * u * t / (need * size) : 0.0;
        double q = 0, d = 0, e = 0, eq = 0, eWheat = 0, eShare = 0, eValue = 0;
        foreach (var s in st)
        {
            double gn = s.BaseNeed * size, x = sraw * s.F;
            q += gn * x; d += gn * System.Math.Min(1.0, x);
            double eg = gn * System.Math.Max(0.0, x - 1.0), egq = eg * s.QtyToR;
            e += eg; eq += egq; eShare += egq * s.SellShare; eValue += egq * s.PriceR;
            if (s.Chain) eWheat += egq;
        }
        double sE = eq > 0 ? eShare / eq : 0.0;
        double Lneed(double tt) => !(need * size > 0) ? 0.0 : (rho * tt * fbar > 0 ? need * size / (rho * tt * fbar) : double.PositiveInfinity);
        double lneed = Lneed(t);
        double kappa = cap > 0 ? 1.0 - System.Math.Min(u, lneed) / cap : nan;
        double raw = cap > 0 ? (u - lneed) / cap : 0.0;
        double unemp = raw > 0 ? System.Math.Min(1.0, raw * (1.0 - sE)) : 0.0;   // 否定形（NaN は 0 へ）
        return new S3Row(sraw, fbar, q, d, e, eWheat, sE, eValue, kappa, q > 0 ? e / q : nan, unemp, size > 0 ? d / size : nan);
    }

    /// <summary>売る財（パンなら小麦粉を遡って小麦）と換算の積：Inputs の最初の入力を入力の無い財まで遡る（深さの上限は財の数）。</summary>
    static (int r, double q) S3SellGood(int g)
    {
        int cur = g; double q = 1.0;
        for (int i = 0; i < Catalog.Goods.Length; i++)
        {
            var inp = Catalog.Goods[cur].Inputs;
            if (inp.Length == 0) break;
            q *= inp[0].QtyPerOutput; cur = inp[0].GoodId;
        }
        return (cur, q);
    }

    /// <summary>四半期の末（<see cref="SubstTable"/> の行の末尾）：自給の段 3-0 の列（13 本）。</summary>
    static void SubstS3Columns(World w, Region r, Coefficients c, int lp, List<string> cols)
    {
        Pop? pp = lp >= 0 && lp < r.Pops.Count ? r.Pops[lp] : null;
        SubstHeads.TryGetValue(r, out var h);
        var (capE, _, _) = S2FarmLabor(r);
        double soldE = Catalog.FarmLaborId < r.Markets.Length ? r.Markets[Catalog.FarmLaborId].LastSold : 0;
        double uE = S2Unsold(soldE, capE);
        var (tE, _) = S2Tech(r, c);
        double rural = 0;
        foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        double landF = r.ArableLand >= 1e8 ? 1.0 : System.Math.Min(1.0, System.Math.Pow(r.ArableLand / System.Math.Max(1.0, rural), c.LandDiminishExponent));   // Territory.LandFactor の式の写し
        var st = new List<S3Staple>(8);
        foreach (var gd in Catalog.Goods)
        {
            if (gd.Tier != GoodTier.Staple || gd.Id >= r.Markets.Length) continue;
            double f = r.HarvestFactor * r.WeatherBandFactor * landF;
            var (rg, qty) = S3SellGood(gd.Id);
            double share = 0, price = 0;
            if (rg < r.Markets.Length)
            {
                var m = r.Markets[rg]; double den = m.LastSold + System.Math.Max(0.0, m.Stock);
                share = den > 0 ? m.LastSold / den : 0.0; price = m.Price;
            }
            st.Add(new S3Staple(gd.BaseNeed, f, share, price, qty, rg != gd.Id));
        }
        double size = pp?.Size ?? 0.0;
        var row = SubstS3Calc(h?.Rho ?? double.NaN, uE, tE, capE, size, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(st));
        double landTax = 0, landRate = w.LandTax;
        foreach (var p in r.Pops)
        {
            if (!(p.SubsistenceBasis > 0)) continue;
            double lb = (c.LandTaxFixedBasis > 0 ? p.LandRentPerMonth + p.LandTaxBasisAdjPerMonth : p.LandRentPerMonth).Value;   // Fiscal.LandTaxBase の式の写し
            if (landRate > 1e-9 && lb > 0) landTax += landRate * lb;
        }
        cols.Add(F(row.Sraw)); cols.Add(F(row.Harvest)); cols.Add(F(row.Q)); cols.Add(F(row.D)); cols.Add(F(row.E)); cols.Add(F(row.EWheat)); cols.Add(F(row.ESoldShare));
        cols.Add(F(row.EValue)); cols.Add(F(row.Kappa)); cols.Add(F(row.KappaE));
        cols.Add(F(row.UnempNew)); cols.Add(F(row.EatPerCap)); cols.Add(F(landTax));
    }
}
