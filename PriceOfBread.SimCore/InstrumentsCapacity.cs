namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (390) 工場の能力の内訳の表 factory_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsFm.cs／InstrumentsAnchor.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （EconomyStep.cs には hook を足さない＝四半期の末に Producer／Market／Region に既に残っている値だけを読む）。国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。
// 目的＝作者への問い Q-PW1〈工場の能力が工場労働者 POP の頭数で決まる式（EconomyStep の Capacity＝労働 POP の Size×fraction×Productivity×boost）が、
//   労働を〈能力〉と〈投入〉の 2 か所に入れる隠れた二重になっていないか〉を、頭数の側と設備・投入の側に分けて読む。
// 読み方（四半期の末の 1 か月の断面・Producer の欄はその月の供給の段が書いた値）：
//   ・能力の鎖（参照価格 Catalog.RefPrices で重みを付けた和・単位＝参照価格の金額/月）：capacity（Producer.Capacity）→ desired（DesiredOutput＝能力×採算）→ filled（desired×LastInputFill）
//     → output（Producer.Output）。priceFactorShare＝desired/capacity・inputFillShare＝filled/desired・outputToFilledShare＝output/filled（在庫循環と産出の慣性を含む残り）。
//     materialFillShare＝desired×LastMaterialFill の和/desired（労働などの役務を含まない素材だけの充足）。inputFillShare と materialFillShare の差が役務（労働）の律速。
//   ・投入の律速の分類（desired の重みで）：notInputBound＝LastInputFill が 1 未満でない／laborBound＝最小の配給率が労働財（労働力・高度労働力・農業労働力）の投入の配給率に一致／
//     otherBound＝最小の配給率が労働以外の投入に一致／unmatchedBound＝どの投入の配給率とも一致しない（Market.LastRation の記帳が追いつかない月＝計器の限界の申告）。
//     laborBoundLoss など＝desired×(1−LastInputFill) の和（その分類が失った量）。
//   ・頭数の側：factoryLaborPopK＝工場の担い手 POP（LaborPopIndex の重複を除いた POP）の Size の和（千人）。capacityRefValuePerPopK＝capacity/factoryLaborPopK。
//     laborSupplyCapacityUnitsPerMonth＝労働力（財）の生産者の Capacity の和（人の側の供給の器）。laborNeedAtCapacityUnitsPerMonth・laborNeedAtDesiredUnitsPerMonth＝工場の
//     能力・作りたい量を作るのに要る労働力（財）の量（QtyPerOutput の和×量）。needAtDesired/supplyCapacity が 1 を超える州の割合は読み手が出す。
//     laborMarketDemandUnitsPerMonth・laborMarketProducedUnitsPerMonth・laborRationMeanShare＝労働力の市場の前月の需要・産出・配給率（需要加重）。
//   ・設備の側：builtScaleSum＝Σ BuiltScale（建てた設備の棟数）・operatingScaleSum＝Σ Scale（操業規模）・industrializationMean＝能力加重の州の生産的な工業化度・
//     steamFillMeanShare／elecFillMeanShare＝能力加重の蒸気・電化の率。
// ⚠限界（計器が自分で申告する）：
//   ①断面は四半期の末の月 1 つ。LastInputFill と LastMaterialFill はその月の終わりに書かれた次月用の値＝その月の産出を決めた値（前月の値）とは 1 か月ずれる。
//      desired はその月の供給の段が書いた値（採算はその月の価格）。output はその月の追従の後の値。
//   ②眠った生産者（操業規模 0 かつ産出 0）は factoryIdleCount に数えて和に入れない（倒産中は factoryBankruptCount）。能力は 0 のはずだが断定はしない。
//   ③投入の律速の分類は Market.LastRation と LastInputFill の double の一致で決める＝本体が ration を書く月と LastRation を書く月が違うと unmatched に落ちる（その量が見える）。
//   ④BuysLabor の生産者は石炭・電力・蒸気機関ほかの動力の生産者を含む（石炭と電力だけを除く StockRow の蒸気の率とは母集団が違う）。
public static partial class Instruments
{
    static readonly string[] CapColNames =
    [
        "factoryCount", "factoryBankruptCount", "factoryIdleCount", "factoryActiveCount",
        "capacityRefValuePerMonth", "desiredRefValuePerMonth", "filledRefValuePerMonth", "materialFilledRefValuePerMonth", "outputRefValuePerMonth",
        "priceFactorShare", "inputFillShare", "materialFillShare", "outputToFilledShare",
        "notInputBoundDesiredRefValuePerMonth", "laborBoundDesiredRefValuePerMonth", "otherBoundDesiredRefValuePerMonth", "unmatchedBoundDesiredRefValuePerMonth",
        "laborBoundLossRefValuePerMonth", "otherBoundLossRefValuePerMonth", "unmatchedBoundLossRefValuePerMonth",
        "factoryLaborPopK", "capacityRefValuePerPopK",
        "laborSupplyCapacityUnitsPerMonth", "laborNeedAtCapacityUnitsPerMonth", "laborNeedAtDesiredUnitsPerMonth",
        "laborMarketDemandUnitsPerMonth", "laborMarketProducedUnitsPerMonth", "laborRationMeanShare",
        "builtScaleSum", "operatingScaleSum", "industrializationMean", "steamFillMeanShare", "elecFillMeanShare",
    ];
    // ★(395) 労働の列を財ごとに分けた末尾の列（(392) 反証役 ic〔v〕中#2〈頭数の側の分子＝労働力・高度労働力・農業労働力の 3 財と分母＝労働力 1 財が非対称・laborBound は技師の不足と工場労働者の不足を同じ分類にする〉への答え）。
    //   既存の 33 列は動かさず末尾へ足す（番人と読み手 an_ic390.py の添字を壊さない）。労働の 3 財は <see cref="LaborGoodIds"/> の順（plainLabor＝労働力・advLabor＝高度労働力・farmLabor＝農業労働力）。
    //   財ごとの 9 列：SupplyCapacityUnitsPerMonth＝その財の生産者の Capacity の和（倒産中を除く）／SupplierPopK＝その財の生産者の担い手 POP（LaborPopIndex の重複を除く）の Size の和（千人）／
    //   NeedAtCapacityUnitsPerMonth・NeedAtDesiredUnitsPerMonth＝工場（BuysLabor）の能力・作りたい量を作るのに要るその財の量（QtyPerOutput×量）／MarketDemandUnitsPerMonth・MarketProducedUnitsPerMonth・RationMeanShare＝その財の市場の前月の需要・産出・配給率（需要加重）／
    //   BoundDesiredRefValuePerMonth・BoundLossRefValuePerMonth＝投入の律速の分類（最小の配給率が労働財の投入に一致）のうち、最小の配給率がその財の投入だった工場の作りたい量と失った量（参照価格の金額/月）。
    //   番人の対照：3 財の BoundDesired の和＝既存の laborBoundDesiredRefValuePerMonth・BoundLoss の和＝laborBoundLossRefValuePerMonth（同じ分類を財ごとに割っただけ）。plainLabor の市場の 3 列＝既存の laborMarket…／laborRationMeanShare と一致。
    // ⚠限界：①最小の配給率が 2 つの労働財で同じなら入力の並びの先の財へ付く。②SupplierPopK の担い手 POP は、ある財の生産者が複数の POP を担い手に持てば全て数える（逆に 1 つの POP が 2 財の担い手でも財ごとに数える＝財をまたぐ和は二重になる）。
    //   ③本体の能力の式が頭数をどう使うかは読まない（頭数は能力の天井でなく供給曲線の尺度＝(392) の訂正）。
    static readonly int[] LaborGoodIds = [Catalog.LaborId, Catalog.AdvLaborId, Catalog.FarmLaborId];
    static readonly string[] LaborGoodPrefix = ["plainLabor", "advLabor", "farmLabor"];
    static readonly string[] LaborColSuffix =
    [
        "SupplyCapacityUnitsPerMonth", "SupplierPopK", "NeedAtCapacityUnitsPerMonth", "NeedAtDesiredUnitsPerMonth",
        "MarketDemandUnitsPerMonth", "MarketProducedUnitsPerMonth", "RationMeanShare", "BoundDesiredRefValuePerMonth", "BoundLossRefValuePerMonth",
    ];
    internal static readonly string CapHeaderLine = "quarter\tyear\tnationUid\tnation\t" + string.Join('\t', CapColNames)
        + "\t" + string.Join('\t', LaborGoodPrefix.SelectMany(p => LaborColSuffix.Select(s => p + s)));

    /// <summary>国の工場の能力の内訳の行（列の順＝<see cref="CapColNames"/>）。番人（テスト）が世界から直に呼べるよう値の組で返す。</summary>
    internal static double[] CapRow(World w)
    {
        int nAll = 0, nBankrupt = 0, nIdle = 0, nActive = 0;
        double cap = 0, des = 0, fil = 0, mfil = 0, outp = 0;
        double dNot = 0, dLab = 0, dOth = 0, dUnm = 0, lLab = 0, lOth = 0, lUnm = 0;
        double popK = 0, labSupply = 0, labNeedCap = 0, labNeedDes = 0, labDem = 0, labProd = 0, labRationW = 0;
        double built = 0, opScale = 0, indW = 0, steamW = 0, elecW = 0;
        int nl = LaborGoodIds.Length;
        var gSup = new double[nl]; var gSupPopK = new double[nl]; var gNeedCap = new double[nl]; var gNeedDes = new double[nl];
        var gDem = new double[nl]; var gProd = new double[nl]; var gRationW = new double[nl]; var gBoundD = new double[nl]; var gBoundL = new double[nl];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            var seenPop = new bool[r.Pops.Count];
            var seenSup = new bool[nl][];
            for (int gi = 0; gi < nl; gi++)
            {
                seenSup[gi] = new bool[r.Pops.Count];
                int lg = LaborGoodIds[gi];
                if (lg < r.Markets.Length)
                {
                    var gm = r.Markets[lg];
                    gDem[gi] += gm.LastDemand; gProd[gi] += gm.LastProduced; gRationW[gi] += gm.LastDemand * gm.LastRation.Value;
                }
            }
            if (Catalog.LaborId < r.Markets.Length)
            {
                var lm = r.Markets[Catalog.LaborId];
                labDem += lm.LastDemand; labProd += lm.LastProduced; labRationW += lm.LastDemand * lm.LastRation.Value;
            }
            double ind = r.ProductiveIndustrialization;
            foreach (var s in r.Producers)
            {
                if (s.GoodId == Catalog.LaborId && !s.Bankrupt) labSupply += s.Capacity;
                if (!s.Bankrupt)
                    for (int gi = 0; gi < nl; gi++)
                    {
                        if (s.GoodId != LaborGoodIds[gi]) continue;
                        gSup[gi] += s.Capacity;
                        int sp = s.LaborPopIndex;
                        if (sp >= 0 && sp < seenSup[gi].Length && !seenSup[gi][sp]) { seenSup[gi][sp] = true; gSupPopK[gi] += r.Pops[sp].Size; }
                    }
                if (!s.BuysLabor) continue;
                nAll++;
                if (s.Bankrupt) { nBankrupt++; continue; }
                if (s.Scale == 0 && s.Output == 0) { nIdle++; continue; }
                nActive++;
                int g = s.GoodId;
                double rp = g >= 0 && g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
                double c = s.Capacity, d = s.DesiredOutput, fill = s.LastInputFill;
                cap += c * rp; des += d * rp; fil += d * fill * rp; mfil += d * s.LastMaterialFill * rp; outp += s.Output * rp;
                // 投入の律速の分類（Market.LastRation と LastInputFill の一致で決める）
                double minRation = 1.0, laborRation = 1.0, qtyLabor = 0; bool hasLabor = false; int laborGi = -1;
                foreach (var inp in s.Inputs)
                {
                    double ra = inp.GoodId >= 0 && inp.GoodId < r.Markets.Length ? r.Markets[inp.GoodId].LastRation.Value : 1.0;
                    if (ra < minRation) minRation = ra;
                    if (ProductionDiag.IsLaborGood(inp.GoodId))
                    {
                        hasLabor = true; qtyLabor += inp.QtyPerOutput;
                        int gi = Array.IndexOf(LaborGoodIds, inp.GoodId);
                        if (gi >= 0) { gNeedCap[gi] += c * inp.QtyPerOutput; gNeedDes[gi] += d * inp.QtyPerOutput; }
                        if (ra < laborRation) { laborRation = ra; laborGi = gi; }
                    }
                }
                labNeedCap += c * qtyLabor; labNeedDes += d * qtyLabor;
                double dv = d * rp, lossV = d * (1.0 - fill) * rp;
                if (!(fill < 1.0 - 1e-9)) dNot += dv;
                else if (hasLabor && Math.Abs(laborRation - fill) <= 1e-12) { dLab += dv; lLab += lossV; if (laborGi >= 0) { gBoundD[laborGi] += dv; gBoundL[laborGi] += lossV; } }
                else if (Math.Abs(minRation - fill) <= 1e-12) { dOth += dv; lOth += lossV; }
                else { dUnm += dv; lUnm += lossV; }
                int lp = s.LaborPopIndex;
                if (lp >= 0 && lp < seenPop.Length && !seenPop[lp]) { seenPop[lp] = true; popK += r.Pops[lp].Size; }
                built += s.BuiltScale; opScale += s.Scale;
                indW += c * rp * ind; steamW += c * rp * s.SteamFill; elecW += c * rp * s.ElecFill;
            }
        }
        double nan = double.NaN;
        var row = new double[]
        {
            nAll, nBankrupt, nIdle, nActive,
            cap, des, fil, mfil, outp,
            cap > 0 ? des / cap : nan, des > 0 ? fil / des : nan, des > 0 ? mfil / des : nan, fil > 0 ? outp / fil : nan,
            dNot, dLab, dOth, dUnm, lLab, lOth, lUnm,
            popK, popK > 0 ? cap / popK : nan,
            labSupply, labNeedCap, labNeedDes, labDem, labProd, labDem > 0 ? labRationW / labDem : nan,
            built, opScale, cap > 0 ? indW / cap : nan, cap > 0 ? steamW / cap : nan, cap > 0 ? elecW / cap : nan,
        };
        // ★(395) 末尾：労働の 3 財ごとの 9 列（既存の 33 列の後ろ・順は LaborGoodIds × LaborColSuffix）
        var all = new double[row.Length + nl * LaborColSuffix.Length];
        row.CopyTo(all, 0);
        for (int gi = 0; gi < nl; gi++)
        {
            int o = row.Length + gi * LaborColSuffix.Length;
            all[o + 0] = gSup[gi]; all[o + 1] = gSupPopK[gi]; all[o + 2] = gNeedCap[gi]; all[o + 3] = gNeedDes[gi];
            all[o + 4] = gDem[gi]; all[o + 5] = gProd[gi]; all[o + 6] = gDem[gi] > 0 ? gRationW[gi] / gDem[gi] : nan;
            all[o + 7] = gBoundD[gi]; all[o + 8] = gBoundL[gi];
        }
        return all;
    }

    static void CapFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;
        if (!(w.TotalPopulation > 0)) return;   // 死んだ国（人口 0）は行を出さない
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var row = CapRow(w);
        sk.Cap ??= Open("factory", seed);
        if (!sk.CapHeader) { sk.Cap.WriteLine(CapHeaderLine); sk.CapHeader = true; }
        sk.Cap.WriteLine(I(q) + "\t" + I(w.Year) + "\t" + I(st.Uid) + "\t" + NameOf(w) + "\t" + string.Join('\t', row.Select(F)));
    }
}
