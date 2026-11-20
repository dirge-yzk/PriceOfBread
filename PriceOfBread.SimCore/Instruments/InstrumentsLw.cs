namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しい状態を足さない。〔履歴 InstrumentsLw.cs#1〕
public static partial class Instruments
{
    internal const string LwHeaderText = "quarter\tyear\tnation\tlawCode\t"
        + "plainCapPerMonth\tplainDemandPerMonth\tplainSoldPerMonth\tplainPriceOverMcIndex\tplainRealPriceIndex\tplainProfitMeanShare\tplainWageBillPerMonth\tplainUnemploymentShare\t"
        + "farmCapPerMonth\tfarmDemandPerMonth\tfarmSoldPerMonth\tfarmPriceOverMcIndex\tfarmRealPriceIndex\tfarmProfitMeanShare\tfarmWageBillPerMonth\tfarmUnemploymentShare\t"
        + "plainFloor0OverSubsIndex\tplainFloor0OverLivIndex\tplainFloor0OverSuffIndex\tplainFloor1OverSubsIndex\tplainFloor1OverLivIndex\tplainFloor1OverSuffIndex\tenactedBindShare"
        + "\tenactedNearFloorShare\tenactedBelowFloorShare\tplainRealPriceMinIndex\tplainTinyPriceRegionShare\tfarmRealPriceMinIndex\tfarmTinyPriceRegionShare"
        + "\tstepBelowShare\tfloorEndOverStepIndex\tpriceEndOverStepIndex\tbelowNoStepShare\tbelowPriceMovedShare\tbelowFloorMovedShare";

    /// <summary>
    /// シミュの状態ではない（ConditionalWeakTable の先例・セーブに載らない）。〔履歴 InstrumentsLw.cs#2〕
    /// </summary>
    private sealed class LwStepRec { public double Price, Floor; public bool Fresh; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, LwStepRec> LwSteps = new();

    /// <summary>
    /// 〔履歴 InstrumentsLw.cs#3〕
    /// </summary>
    internal static void LwStep(Region r, int goodId, double price, double floor)
    {
        if (!Tg.Lw) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (goodId != Catalog.LaborId) return;
        var rec = LwSteps.GetOrCreateValue(r);
        rec.Price = price; rec.Floor = floor; rec.Fresh = true;
    }

    private sealed class LwAcc
    {
        public double Cap, Demand, Sold, PxMcW, RealW, ProfW, Bill, UnW, UnDen, CapW;
        public double MinReal = double.PositiveInfinity, TinyCap;   // MinReal〔履歴 InstrumentsLw.cs#4〕
    }

    static void LwFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!Tg.Lw) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        var plain = new LwAcc(); var farm = new LwAcc();
        var fl = new double[6]; double flW = 0, bindW = 0, bindCap = 0, nearW = 0, belowW = 0;
        double stBelowW = 0, stFloorCap = 0, stFloorRatioW = 0, stPriceRatioW = 0, stSeenCap = 0, noStepW = 0, priceMovedW = 0, floorMovedW = 0;   // stBelowW〔履歴 InstrumentsLw.cs#5〕
        foreach (var r in w.Regions)
        {
            LwGood(r, w, c, Catalog.LaborId, plain, out double capP, out int lpP);
            LwGood(r, w, c, Catalog.FarmLaborId, farm, out _, out _);
            if (!(capP > 0) || lpP < 0 || lpP >= r.Pops.Count) continue;
            double price = r.Markets[Catalog.LaborId].Price;
            if (!(price > 0)) continue;
            double size = r.Pops[lpP].Size, sold = r.Markets[Catalog.LaborId].LastSold;
            for (int k = 0; k < 3; k++)
            {
                double basket = MinimumWageFloor.BasketPerPerson(r, (MinimumWageLaw)(k + 1));
                fl[k] += capP * (basket * size / capP) / price;
                if (sold > 1e-9) fl[3 + k] += capP * (basket * size / sold) / price;
            }
            flW += capP;
            if (w.MinimumWage != MinimumWageLaw.None)
            {
                bindCap += capP;
                if (MinimumWageFloor.PerUnit(r, Catalog.LaborId, w.MinimumWage) > price) bindW += capP;
                // 縛り＝値段が床の 1.05 倍以下／床の下＝値段が床の 0.90 倍未満〔履歴 InstrumentsLw.cs#6〕
                double ceil = r.GoodOf(Catalog.LaborId)!.RefPrice * r.PriceLevel * MarketFlow.EffWageMax(r, c);
                double fl0 = MinimumWageFloor.AbsFloor(r, Catalog.LaborId, ceil, w.MinimumWage);
                if (fl0 > 0 && price <= fl0 * 1.05) nearW += capP;
                if (fl0 > 0 && price < fl0 * 0.90) belowW += capP;
                // seen〔履歴 InstrumentsLw.cs#7〕
                bool seen = LwSteps.TryGetValue(r, out var srec) && srec.Fresh;
                if (seen)
                {
                    stSeenCap += capP;
                    if (srec!.Floor > 0) { stFloorCap += capP; if (srec.Price < srec.Floor * 0.90) stBelowW += capP; stFloorRatioW += capP * (fl0 / srec.Floor); }
                    if (srec.Price > 0) stPriceRatioW += capP * (price / srec.Price);
                }
                if (fl0 > 0 && price < fl0 * 0.90)
                {
                    if (!seen || !(srec!.Floor > 0)) noStepW += capP;
                    else if (srec.Price >= fl0 * 0.90) priceMovedW += capP;
                    else floorMovedW += capP;
                }
            }
            if (LwSteps.TryGetValue(r, out var srec2)) srec2.Fresh = false;
        }
        double Div(double a, double b) => b > 0 ? a / b : double.NaN;
        sk.Lab ??= Open("labor", seed);
        if (!sk.LabHeader) { sk.Lab.WriteLine(LwHeaderText); sk.LabHeader = true; }
        var cols = new List<string>
        {
            q.ToString(System.Globalization.CultureInfo.InvariantCulture), w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), NameOf(w), ((int)w.MinimumWage).ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (var a in new[] { plain, farm })
            cols.AddRange([F(a.Cap), F(a.Demand), F(a.Sold), F(Div(a.PxMcW, a.CapW)), F(Div(a.RealW, a.CapW)), F(Div(a.ProfW, a.CapW)), F(a.Bill), F(Div(a.UnW, a.UnDen))]);
        for (int k = 0; k < 6; k++) cols.Add(F(Div(fl[k], flW)));
        cols.Add(F(bindCap > 0 ? bindW / bindCap : 0.0));
        cols.Add(F(bindCap > 0 ? nearW / bindCap : 0.0));
        cols.Add(F(bindCap > 0 ? belowW / bindCap : 0.0));
        var tailCols = new[] { F(bindCap > 0 ? stBelowW / bindCap : 0.0), F(Div(stFloorRatioW, stFloorCap)), F(Div(stPriceRatioW, stSeenCap)), F(bindCap > 0 ? noStepW / bindCap : 0.0), F(bindCap > 0 ? priceMovedW / bindCap : 0.0), F(bindCap > 0 ? floorMovedW / bindCap : 0.0) };
        foreach (var a in new[] { plain, farm })
        {
            cols.Add(F(a.MinReal));
            cols.Add(F(Div(a.TinyCap, a.CapW)));
        }
        cols.AddRange(tailCols);
        sk.Lab.WriteLine(string.Join('\t', cols));
    }

    /// <summary>1 州の 1 つの労働財（人力の生産者）を a に足す。cap＝その州の人力の能力の和・lp＝最初の人力の生産者の担い手 POP の添字（無ければ −1）。</summary>
    private static void LwGood(Region r, World w, Coefficients c, int goodId, LwAcc a, out double cap, out int lp)
    {
        cap = 0; lp = -1;
        if (goodId >= r.Markets.Length) return;
        var m = r.Markets[goodId];
        double price = m.Price;
        foreach (var s in r.Producers)
        {
            if (s.GoodId != goodId || !EconomyStep.IsHumanLaborSupply(s) || s.Bankrupt) continue;
            if (lp < 0) lp = s.LaborPopIndex;
            if (!(s.Capacity > 0)) continue;
            double mc = EconomyStep.MineralSelfCost(s, r, c, w) + EconomyStep.InputCostPerUnit(s, r, r, c);
            cap += s.Capacity; a.CapW += s.Capacity;
            if (mc > 0) a.PxMcW += s.Capacity * price / mc;
            if (r.PriceLevel > 0) a.RealW += s.Capacity * price / r.PriceLevel;
            a.ProfW += s.Capacity * EconomyStep.ProfitFactor(s, price, mc, c);
        }
        if (lp < 0) return;
        if (cap > 0 && r.PriceLevel > 0)
        {   // 注〔履歴 InstrumentsLw.cs#8〕
            double real = price / r.PriceLevel;
            if (real < a.MinReal) a.MinReal = real;
            if (real < 1e-3) a.TinyCap += cap;
        }
        a.Cap += cap; a.Demand += m.LastDemand; a.Sold += m.LastSold; a.Bill += m.LastSold * price;
        if (lp < r.Pops.Count) { double sz = r.Pops[lp].Size; a.UnW += sz * r.Pops[lp].UnemploymentRate.Value; a.UnDen += sz; }
    }
}
