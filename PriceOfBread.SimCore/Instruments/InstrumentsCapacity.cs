namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない （EconomyStep.cs には hook を足さない＝四半期の末に Producer／Market／Region に既に残っている値だけを読む）。〔履歴 InstrumentsCapacity.cs#1〕
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
    // 既存の 33 列は動かさず末尾へ足す（番人と読み手 an_ic390.py の添字を壊さない）。〔履歴 InstrumentsCapacity.cs#2〕
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
                    if (Catalog.IsLaborGood(inp.GoodId))
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
        // all〔履歴 InstrumentsCapacity.cs#3〕
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
