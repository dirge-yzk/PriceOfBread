using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (837) 地主の政治 C1 の恒等の計器 cornint_s（読むだけ・恒等・事前登録 measurements/m837_20261104_prereg_ss.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCearn.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝c66ds 下書き §3 C1：穀物法と関税の地主の項（今は RentProtectionWeightCoef×Rent[b]）の向きを、持ち分の作物が輸入と競うか輸出かの割合 σ で決める案の材料。
//   今の値（PolicyPreferences の式を計器が自前で書き直す）と C1 の値を同じ盤面で両方出し、向きの食い違いを数える。
// エンジンの関数を呼ばない：ブロックの分け方・地代の和・慣習の取り分は計器が自分で書く。エンジンの状態は欄（POP の LandRentPerMonth・生産者の Output・RentShare・州の FarmRentCap・市場の LastProduced・値段・World.ImportsByGood・ExportsByGood）として読むだけ。
// 行＝国×四半期（全ての国）。額は国の通貨・どれも直近の月の値（…PerMonth）。ブロックの添字 b＝0 旧支配層・1 近代エリート・2 大衆（PolicyPreferences.BlocOf と同じ三項式）。
// σ_g＝(輸入−輸出)÷(輸入＋輸出＋産出の値)。輸入と輸出は World.ImportsByGood・ExportsByGood＝国境を越えた分だけ（BilateralMarket が crossA の中でだけ積む＝同じ国の州どうしは数えない）。産出の値＝Σ州 LastProduced×値段。
// 地代の財ごとの配分：国内＝POP の LandRentPerMonth をその POP が所有する RGO の産出の値で按分（所有する RGO の産出が 0 なら rentDomUnattrPerMonth へ）・輸出＝PaySales の地代の近似（輸出された財の RGO の売り手の産出比で ExportsByGood を割り、慣習の取り分 min(RentShare, FarmRentCap) を所有者のブロックへ）。
// 穀物法の客体＝Tier が Staple か GrainChain の財（BilateralMarket の cornPen と同じ客体）。
// ⚠限界（計器が自己申告する）：①四半期の末＝直近の月の値②国内の地代の財ごとの配分は産地ごとの地代がどこにも貯まっていないための近似③輸出の地代は国営持分・不在地主・州ごとの取引の分布を数えない国の産出比の近似④ByGood は値（世界価格建て・輸入は運賃を含む）で産出は州の値段建て
//   ⑤国内の地代の和は国の全州の POP の max(0, LandRentPerMonth)（エンジンの Build の第 3 走査と同じ集合かは試験 C5 が確かめる）⑥ByGood の配列が無い国（多国シム未走行）は σ と C1 の列が nan。
public static partial class Instruments
{
    sealed class CornintSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CornintSink> CornintSinks = new();

    /// <summary>ブロックの添字（純関数）：0 旧支配層（Closed）・1 近代エリート・2 大衆。PolicyPreferences.BlocOf と同じ三項式を計器が自分で書く。</summary>
    internal static int CnBloc(Pop p) => p.Closed ? 0 : p.IsModernElite ? 1 : 2;

    /// <summary>σ（純関数）＝(輸入−輸出)÷(輸入＋輸出＋産出)。分母が 0 以下なら 0。輸入超過で正・輸出超過で負。</summary>
    internal static double CnSigma(double imp, double exp, double prod)
    {
        double den = imp + exp + prod;
        return den > 0 ? (imp - exp) / den : 0.0;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の行を書く。</summary>
    static void CornintTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = CornintSinks.GetValue(sim, _ => new CornintSink());
        sk.W ??= Open("cornint", seed);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int gc = Catalog.Goods.Length;
        var isCorn = new bool[gc];
        for (int g = 0; g < gc; g++) isCorn[g] = Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].GrainChain;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            var c = w.Coef;
            var rentDom = new double[3]; var rentUnattr = new double[3]; var rentGrainDom = new double[3]; var rentOtherDom = new double[3];
            var rentExp = new double[3]; var rentGrainExp = new double[3];
            var rentBg = new double[3, gc];
            var prodVal = new double[gc]; var totalOut = new double[gc];
            // 国内：POP の地代をその POP が所有する RGO の産出の値で按分する。産出の値と売り手の産出も同じ走査で数える
            foreach (var r in w.Regions)
            {
                int ng = System.Math.Min(r.Markets.Length, gc);
                for (int g = 0; g < ng; g++) prodVal[g] += r.Markets[g].LastProduced * r.Markets[g].Price;
                var ownerTot = new double[r.Pops.Count];
                foreach (var s in r.Producers)
                {
                    if (s.GoodId < 0 || s.GoodId >= ng) continue;
                    if (s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && s.LaborPopIndex >= 0 && s.LaborPopIndex < r.Pops.Count) totalOut[s.GoodId] += System.Math.Max(0, s.Output);
                    if (!s.IsRgo || s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count) continue;
                    ownerTot[s.OwnerPopIndex] += System.Math.Max(0, s.Output * r.Markets[s.GoodId].Price);
                }
                for (int pi = 0; pi < r.Pops.Count; pi++)
                {
                    var p = r.Pops[pi]; int b = CnBloc(p);
                    double rent = System.Math.Max(0, p.LandRentPerMonth.Value);
                    if (rent <= 0) continue;
                    rentDom[b] += rent;
                    if (!(ownerTot[pi] > 0)) { rentUnattr[b] += rent; continue; }
                    foreach (var s in r.Producers)
                    {
                        if (!s.IsRgo || s.OwnerPopIndex != pi || s.GoodId < 0 || s.GoodId >= ng) continue;
                        double v = System.Math.Max(0, s.Output * r.Markets[s.GoodId].Price);
                        if (!(v > 0)) continue;
                        double share = rent * v / ownerTot[pi];
                        rentBg[b, s.GoodId] += share;
                        if (isCorn[s.GoodId]) rentGrainDom[b] += share; else rentOtherDom[b] += share;
                    }
                }
            }
            // 輸出：PaySales の地代の近似
            bool haveTrade = w.ExportsByGood is { } eg0 && w.ImportsByGood is { } ig0 && eg0.Length == gc && ig0.Length == gc;
            double[]? exp = haveTrade ? w.ExportsByGood : null, imp = haveTrade ? w.ImportsByGood : null;
            if (haveTrade)
                foreach (var r in w.Regions)
                {
                    int ng = System.Math.Min(r.Markets.Length, gc);
                    foreach (var s in r.Producers)
                    {
                        if (!s.IsRgo || s.GoodId < 0 || s.GoodId >= ng || !(exp![s.GoodId] > 0) || !(totalOut[s.GoodId] > 0)) continue;
                        if (s.OwnerPopIndex < 0 || s.OwnerPopIndex >= r.Pops.Count || s.LaborPopIndex < 0 || s.LaborPopIndex >= r.Pops.Count) continue;
                        double amt = exp[s.GoodId] * System.Math.Max(0, s.Output) / totalOut[s.GoodId];
                        if (!(amt > 0)) continue;
                        double rent = System.Math.Min(s.RentShare.Value, r.FarmRentCap.Value) * amt;
                        if (!(rent > 0)) continue;
                        int b = CnBloc(r.Pops[s.OwnerPopIndex]);
                        rentExp[b] += rent; rentBg[b, s.GoodId] += rent;
                        if (isCorn[s.GoodId]) rentGrainExp[b] += rent;
                    }
                }
            double tr = rentDom[0] + rentDom[1] + rentDom[2];
            double rentTotal = tr + rentExp[0] + rentExp[1] + rentExp[2];
            var engShare = new double[3];
            for (int b = 0; b < 3; b++) engShare[b] = tr > 1e-9 ? rentDom[b] / tr : rentDom[b];   // Normalize と同じ（分母が 1e-9 以下なら割らない）
            // 貿易と σ
            double gImp = 0, gExp = 0, gProd = 0, aImp = 0, aExp = 0;
            double sigRwGrainNum = 0, sigRwGrainDen = 0, sigRwAllNum = 0, sigRwAllDen = 0;
            var intGrain = new double[3]; var intAll = new double[3];
            if (haveTrade)
                for (int g = 0; g < gc; g++)
                {
                    double sg = CnSigma(imp![g], exp![g], prodVal[g]);
                    aImp += imp[g]; aExp += exp[g];
                    if (isCorn[g]) { gImp += imp[g]; gExp += exp[g]; gProd += prodVal[g]; }
                    double rg = rentBg[0, g] + rentBg[1, g] + rentBg[2, g];
                    sigRwAllNum += rg * sg; sigRwAllDen += rg;
                    if (isCorn[g]) { sigRwGrainNum += rg * sg; sigRwGrainDen += rg; }
                    if (rentTotal > 0)
                        for (int b = 0; b < 3; b++)
                        {
                            intAll[b] += rentBg[b, g] * sg / rentTotal;
                            if (isCorn[g]) intGrain[b] += rentBg[b, g] * sg / rentTotal;
                        }
                }
            double nan = double.NaN;
            double coefRent = c.RentProtectionWeightCoef;
            var cols = new List<(string Name, string Val)>(64)
            {
                ("quarter", I(q)), ("year", I(w.Year)), ("nation", NameOf(w)), ("cornLawsCode", I((int)w.CornLaws)),
            };
            void Add3(string name, double[] a) { for (int b = 0; b < 3; b++) cols.Add((name + b, F(a[b]))); }
            Add3("rentDomPerMonth", rentDom); cols.Add(("rentDomTotalPerMonth", F(tr))); Add3("rentDomUnattrPerMonth", rentUnattr); Add3("rentOtherDomPerMonth", rentOtherDom); Add3("engineRentShare", engShare);
            Add3("rentExpPerMonth", rentExp); Add3("rentGrainDomPerMonth", rentGrainDom); Add3("rentGrainExpPerMonth", rentGrainExp);
            var tariffNow = new double[3]; var cornNow = new double[3]; var c1Corn = new double[3]; var c1Tariff = new double[3];
            for (int b = 0; b < 3; b++)
            {
                tariffNow[b] = coefRent * engShare[b];
                cornNow[b] = coefRent * engShare[b] - (b == 0 ? 0.0 : 1.0);
                c1Corn[b] = haveTrade ? coefRent * intGrain[b] - (b == 0 ? 0.0 : 1.0) : nan;
                c1Tariff[b] = haveTrade ? coefRent * intAll[b] : nan;
            }
            Add3("tariffLandTermNow", tariffNow); Add3("cornPrefNow", cornNow);
            cols.Add(("grainImportValue", F(haveTrade ? gImp : nan))); cols.Add(("grainExportValue", F(haveTrade ? gExp : nan))); cols.Add(("grainProducedValue", F(haveTrade ? gProd : nan)));
            cols.Add(("allImportValue", F(haveTrade ? aImp : nan))); cols.Add(("allExportValue", F(haveTrade ? aExp : nan)));
            cols.Add(("sigmaGrain", F(haveTrade && gImp + gExp + gProd > 0 ? (gImp - gExp) / (gImp + gExp + gProd) : nan)));
            cols.Add(("sigmaGrainRentW", F(haveTrade && sigRwGrainDen > 0 ? sigRwGrainNum / sigRwGrainDen : nan)));
            cols.Add(("sigmaAllRentW", F(haveTrade && sigRwAllDen > 0 ? sigRwAllNum / sigRwAllDen : nan)));
            Add3("c1InterestGrain", haveTrade ? intGrain : [nan, nan, nan]); Add3("c1InterestAll", haveTrade ? intAll : [nan, nan, nan]);
            Add3("c1CornPref", c1Corn); Add3("c1TariffLand", c1Tariff);
            cols.Add(("flipCorn0", haveTrade ? I((cornNow[0] > 0) != (c1Corn[0] > 0) ? 1 : 0) : "nan"));
            if (!sk.Header) { sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Name))); sk.Header = true; }
            sk.W.WriteLine(string.Join('\t', cols.ConvertAll(x => x.Val)));
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void CornintClose(MultiNationSim sim)
    {
        if (CornintSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
