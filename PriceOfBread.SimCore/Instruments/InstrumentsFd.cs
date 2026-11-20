namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsFd.cs#1〕
public static partial class Instruments
{
    internal const string FdHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tstapleNeedPerMonth\tmarketNeedPerMonth\tdesiredPerMonth\tdesiredOldPerMonth\tdemandPerMonth\tsubPerMonth\toldMetPerMonth\tcarryPerMonth\tcarryPopK\t"
        + "fillOld\tfillNew\tbelowOldK\tbelowNewK\tharvestMean\tsubsistenceMean\tclampBindShare\tweatherHarvestMean\tlandFactor\toverNeedPopK\tflags";

    /// <summary>主食の台帳の蓄え（シミュの状態ではない・セーブに載らない・ConditionalWeakTable の先例）。書くのは EconomyStep の INS=1 の月だけ・読むのは FdFold だけ。</summary>
    internal sealed class FdDiag
    {
        public double MarketNeed, Desired, DesiredOld, Demand, Sub, OldMet, Carry, CarryPopK, StapleNeed, PopK, FillOldW, FillNewW, FillPopK, BelowOldK, BelowNewK, SubsW, OverNeedPopK;
        public double HarvestMean = double.NaN, ClampBindShare = double.NaN, WeatherHarvestMean = double.NaN, LandFactor = double.NaN;
        public bool Written;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, FdDiag> FdDiags = new();
    internal static FdDiag? FdDiagOf(Region r) => FdDiags.TryGetValue(r, out var d) ? d : null;

    /// <summary>消費の月の始め：蓄えを 0 に戻す（その月の主食の和を溜め直す）。</summary>
    internal static FdDiag FdBegin(Region r)
    {
        var d = FdDiags.GetOrCreateValue(r);
        d.MarketNeed = d.Desired = d.DesiredOld = d.Demand = d.Sub = d.OldMet = d.Carry = d.CarryPopK = d.StapleNeed = d.PopK = d.FillOldW = d.FillNewW = d.FillPopK = d.BelowOldK = d.BelowNewK = d.SubsW = d.OverNeedPopK = 0;
        d.HarvestMean = d.ClampBindShare = d.WeatherHarvestMean = d.LandFactor = double.NaN; d.Written = true;
        return d;
    }

    /// <summary>消費の月の終わり（①の締めの後）：POP ごとに旧の台帳の FoodMet（<paramref name="oldMet"/>）と新の台帳の FoodMet から充足を求めて溜める。</summary>
    internal static void FdPop(FdDiag d, Pop p, double oldMet)
    {
        if (!Tg.Fd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        d.PopK += p.Size; d.StapleNeed += p.FoodNeed; d.OldMet += oldMet; d.Carry += p.FoodMet - oldMet; d.SubsW += p.SubsistenceShare * p.FoodNeed;
        if (p.FoodMet - oldMet > 0) d.CarryPopK += p.Size;
        if (p.FoodMet > p.FoodNeed * (1 + 1e-9)) d.OverNeedPopK += p.Size;   // 追記 2：台帳の不変条件 S1（FoodMet≦FoodNeed）の観測。下の Clamp01 は違反を隠すのでここで数える
        if (p.FoodNeed > 1e-9)
        {
            double fo = PoliticalQuantities.Clamp01(oldMet / p.FoodNeed), fn = PoliticalQuantities.Clamp01(p.FoodMet / p.FoodNeed);
            d.FillPopK += p.Size; d.FillOldW += p.Size * fo; d.FillNewW += p.Size * fn;
            if (fo < 0.8) d.BelowOldK += p.Size;
            if (fn < 0.8) d.BelowNewK += p.Size;
        }
    }

    static void FdFold(Sink sk, ulong seed, World w, int q, bool picked)
    {
        if (!Tg.Fd) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        var c = w.Coef;
        if (!(c.StapleLedgerSum > 0 || c.StapleDemandRebalance > 0 || c.HarvestShortfallToMarket > 0) || !picked || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
        foreach (var r in w.Regions)
        {
            var d = FdDiagOf(r);
            if (d is null || !d.Written || !(d.PopK > 0)) continue;
            double Div(double a, double b) => b > 0 ? a / b : double.NaN;
            sk.Fd ??= Open("fd", seed);
            if (!sk.FdHeader) { sk.Fd.WriteLine(FdHeaderText); sk.FdHeader = true; }
            sk.Fd.WriteLine(string.Join('\t', I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), F(d.PopK), F(d.StapleNeed), F(d.MarketNeed), F(d.Desired), F(d.DesiredOld), F(d.Demand), F(d.Sub), F(d.OldMet), F(d.Carry), F(d.CarryPopK),
                F(Div(d.FillOldW, d.FillPopK)), F(Div(d.FillNewW, d.FillPopK)), F(d.BelowOldK), F(d.BelowNewK), F(d.HarvestMean), F(Div(d.SubsW, d.StapleNeed)), F(d.ClampBindShare), F(d.WeatherHarvestMean), F(d.LandFactor), F(d.OverNeedPopK),
                I((c.StapleLedgerSum > 0 ? 1 : 0) + (c.StapleDemandRebalance > 0 ? 2 : 0) + (c.HarvestShortfallToMarket > 0 ? 4 : 0))));
        }
    }
}
