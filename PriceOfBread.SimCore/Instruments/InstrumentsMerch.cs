using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（置き場は ConditionalWeakTable だけ）。〔履歴 InstrumentsMerch.cs#1〕
public static partial class Instruments
{
    internal const string MerchHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tmerchantK\tartisanK\tworkerK\tmerchantEmpty\tartisanEmpty\t"
        + "avgPerCap\tworkerSavPerCap\tmeritActive\tlockBOpen\tlockABlockedMerchant\tlockABlockedArtisan\t"
        + "merchEarnedPerCap\tworkerIncomePerCap\temptyMerchantEarnedPerMonth\tmerchantDebt\tpromotionPremium"
        + "\tmerchSavPerCapAtGate\tmerchFallGateIn\tmerchPettyIn\tartisanPettyIn\tmerchCapRiseOut\tmerchFallOut\tartisanFallOut";   // tmerchSavPerCapAtGate〔履歴 InstrumentsMerch.cs#2〕

    /// <summary>空の判定の閾値（エンジンの Meritocracy.Eps と同じ値の写し・係数ではない）。</summary>
    internal const double MerchEps = 1e-9;

    sealed class MerchBuf { public readonly List<string> Rows = new(); public readonly List<MerchStepAcc> Steps = new(); }

    /// <summary>
    /// 道の量は実際に移った人数（移す前後の POP の Size の差）。〔履歴 InstrumentsMerch.cs#3〕
    /// </summary>
    internal sealed class MerchStepAcc
    {
        public readonly double[] Route = new double[5];
        public double SavPerCap = double.NaN, GateIn = double.NaN;
        public void Reset() { System.Array.Clear(Route); SavPerCap = double.NaN; GateIn = double.NaN; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, MerchStepAcc> MerchSteps = new();

    /// <summary>
    /// moved＝実際に移った人数（正）。〔履歴 InstrumentsMerch.cs#4〕
    /// </summary>
    internal static void MerchRoute(Region r, int route, double moved)
    {
        MerchSteps.GetValue(r, _ => new MerchStepAcc()).Route[route] += moved;
    }

    /// <summary>
    /// アンカー＝f≦0 なら州の平均資産（Σmax(0,貯蓄)÷Σ人数・門の時点で再計算）・f＞0 なら (1−f)×平均＋f×自分の年所得÷人数。〔履歴 InstrumentsMerch.cs#5〕
    /// </summary>
    internal static void MerchGate(Region r, Pop mt, Coefficients c)
    {
        var a = MerchSteps.GetValue(r, _ => new MerchStepAcc());
        double total = 0, sav = 0;
        foreach (var p in r.Pops) { total += p.Size; sav += System.Math.Max(0, p.Savings.Value); }
        double avg = total > 0 ? sav / total : 0;
        double f = c.FallAnchorReform, anchor = avg;
        if (f > 0) anchor = (1.0 - f) * avg + f * (Calendar.PerMonthToPerYear(System.Math.Max(0, System.Math.Max(mt.EarnedPerMonth.Value, mt.LastEarnedPerMonth.Value))) / System.Math.Max(MerchEps, mt.Size));
        double pc = mt.Savings.Value / System.Math.Max(MerchEps, mt.Size);
        a.SavPerCap = mt.Size > MerchEps ? mt.Savings.Value / mt.Size : double.NaN;
        a.GateIn = mt.Size > MerchEps && pc < c.MiddleFallThreshold * anchor ? 1 : 0;
    }

    /// <summary>
    /// 〔履歴 InstrumentsMerch.cs#6〕
    /// </summary>
    internal static (double[] Route, double SavPerCap, double GateIn) MerchStepGet(Region r)
        => MerchSteps.TryGetValue(r, out var a) ? ((double[])a.Route.Clone(), a.SavPerCap, a.GateIn) : (new double[5], double.NaN, double.NaN);
    sealed class MerchSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, MerchBuf> MerchBufs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, MerchSink> MerchSinks = new();

    /// <summary>1 つの州の再計算の結果（純関数の返り値）。</summary>
    internal readonly record struct MerchLocksRow(double PopK, double MerchantK, double ArtisanK, double WorkerK, bool MerchantEmpty, bool ArtisanEmpty,
        double AvgPerCap, double WorkerSavPerCap, bool MeritActive, bool LockBOpen, double MerchEarnedPerCap, double WorkerIncomePerCap, double EmptyMerchantEarned, double MerchantDebt);

    /// <summary>州の POP から錠 A・B を再計算する（読むだけ・エンジンの関数は呼ばない）。<paramref name="pettyEntryRate"/>＝小営業の入口の四半期率（係数の値）。</summary>
    internal static MerchLocksRow MerchLocks(Region r, double pettyEntryRate)
    {
        double total = 0, totalSav = 0;
        foreach (var p in r.Pops) { total += p.Size; totalSav += System.Math.Max(0, p.Savings.Value); }
        double avgPerCap = total > 0 ? totalSav / total : 0;
        Pop? merchant = r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count ? r.Pops[r.MerchantPopIndex] : null;
        Pop? artisan = r.ArtisanPopIndex >= 0 && r.ArtisanPopIndex < r.Pops.Count ? r.Pops[r.ArtisanPopIndex] : null;
        Pop? worker = r.MeritFromIndex >= 0 && r.MeritFromIndex < r.Pops.Count ? r.Pops[r.MeritFromIndex] : null;
        double mk = merchant?.Size ?? 0, ak = artisan?.Size ?? 0, wk = worker?.Size ?? 0;
        bool mEmpty = !(mk > MerchEps), aEmpty = !(ak > MerchEps);
        double wSavPc = wk > MerchEps ? worker!.Savings.Value / wk : double.NaN;
        bool meritActive = total > MerchEps && r.MeritFromIndex >= 0 && r.MeritToIndex >= 0;
        bool lockB = meritActive && pettyEntryRate > 0 && avgPerCap > MerchEps && wk > MerchEps && wSavPc >= avgPerCap;
        double mEarn = merchant is null ? 0 : merchant.EarnedPerMonth.Value;
        return new MerchLocksRow(total, mk, ak, wk, mEmpty, aEmpty, avgPerCap, wSavPc, meritActive, lockB,
            mk > MerchEps ? mEarn / mk : double.NaN, wk > MerchEps ? worker!.EarnedPerMonth.Value / wk : double.NaN,
            mEmpty ? mEarn : double.NaN, merchant?.Debt.Value ?? 0);
    }

    /// <summary>社会移動の区間の前（<see cref="Simulation"/>）：この国の全ての州の行を置く（直近の 1 四半期ぶんだけ・次の呼びで置き換える）。助走中は何もしない。</summary>
    internal static void MerchEnter(World w)
    {
        if (!On || w.WarmupFrozen) return;
        var buf = MerchBufs.GetValue(w, _ => new MerchBuf());
        buf.Rows.Clear(); buf.Steps.Clear();
        string nation = NameOf(w); double pettyRate = w.Coef.PettyEntryRatePerQuarter.Value, premium = w.Coef.PromotionPremium;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string B(bool v) => v ? "1" : "0";
        foreach (var r in w.Regions)
        {
            var m = MerchLocks(r, pettyRate);
            var stepAcc = MerchSteps.GetValue(r, _ => new MerchStepAcc()); stepAcc.Reset(); buf.Steps.Add(stepAcc);   // stepAcc〔履歴 InstrumentsMerch.cs#7〕
            buf.Rows.Add(string.Join("\t", I(w.Year), nation, r.Name, r.Tier.ToString(), F(m.PopK), F(m.MerchantK), F(m.ArtisanK), F(m.WorkerK), B(m.MerchantEmpty), B(m.ArtisanEmpty),
                F(m.AvgPerCap), F(m.WorkerSavPerCap), B(m.MeritActive), B(m.LockBOpen), B(m.LockBOpen && m.MerchantEmpty), B(m.LockBOpen && m.ArtisanEmpty),
                F(m.MerchEarnedPerCap), F(m.WorkerIncomePerCap), F(m.EmptyMerchantEarned), F(m.MerchantDebt), F(premium)));
        }
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：置いてあった行に q を付けて書き、空にする。</summary>
    static void MerchTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = MerchSinks.GetValue(sim, _ => new MerchSink());
        sk.W ??= Open("merch", seed);
        if (!sk.Header) { sk.W.WriteLine(MerchHeaderText); sk.Header = true; }
        string qs = q.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            if (!MerchBufs.TryGetValue(n.World, out var buf)) continue;
            for (int i = 0; i < buf.Rows.Count; i++)
            {
                var st = buf.Steps[i];
                sk.W.Write(qs); sk.W.Write('\t'); sk.W.Write(buf.Rows[i]);
                sk.W.Write('\t'); sk.W.Write(F(st.SavPerCap)); sk.W.Write('\t'); sk.W.Write(F(st.GateIn));
                foreach (double v in st.Route) { sk.W.Write('\t'); sk.W.Write(F(v)); }
                sk.W.WriteLine();
            }
            buf.Rows.Clear(); buf.Steps.Clear();
        }
    }

    static void MerchClose(MultiNationSim sim)
    {
        if (MerchSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
