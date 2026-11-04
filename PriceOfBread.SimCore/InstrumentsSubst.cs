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
        + "stapleNeedWeightedPrice\tstapleNeedValuePerMonth\tmarketShareOfNeed\tobligationOverMarketNeed";

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
                    if (p.Subsistence > 0)
                    {
                        subPop += p.Size; subWeighted += p.Size * p.Subsistence; rentFarmers += rent;
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
                sk.W.WriteLine(string.Join('\t', cols));
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void SubstClose(MultiNationSim sim)
    {
        if (SubstSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
