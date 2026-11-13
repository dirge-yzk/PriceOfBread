namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。〔履歴 InstrumentsHabitlg.cs#1〕
public static partial class Instruments
{
    internal const string HabitlgHeaderText = "quarter	year	nationIndex	nation	region	monthsCount	logisticFlag	pcMeanPerMonth	pcMinPerMonth	pcMaxPerMonth	stockLastIndex	stockMaxIndex	oldNextLastIndex	logNextLastIndex	transCount	oldMismatchCount	logMismatchCount	lgViolMonthsCount	lgViolMaxIndex	pinMonthsCount	floorMonthsCount	overDMonthsCount";

    sealed class HabitlgAcc
    {
        public int Months, Trans, OldMis, LogMis, Viol, Pin, Floor, OverD; public bool Seen, HasPrev;
        public double PcSum, PcMin = double.PositiveInfinity, PcMax, StockMax, OldLast = double.NaN, LogLast = double.NaN, ViolMax;
        public double PrevS, PrevPc; public Coefficients? PrevCoef;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, HabitlgAcc> HabitlgAccs = new();
    // HabitcapR〔履歴 InstrumentsHabitlg.cs#2〕
    static string HabitcapR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    sealed class HabitcapSink { public bool Started; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, HabitcapSink> HabitlgSinks = new();

    /// <summary>旧（線形）と新（ロジスティック）の挟む前の値と挟んだ値。d は依存者 1 人の上乗せの量（+∞ も受ける純関数）。挟みは 0〜d・NaN は 0（エンジンの ClampOr と同じ規則を計器の側で書く）。</summary>
    internal static (double Old, double Log, double LogRaw) HabitLgPair(double g, double dcy, double d, double s, double pc)
    {
        double oldRaw = s + g * pc - dcy * s;
        double logRaw = s + g * pc * (1 - s / d) - dcy * s;
        return (Clip(oldRaw, d), Clip(logRaw, d), logRaw);
        static double Clip(double x, double hi) => x < 0 ? 0 : x > hi ? hi : (x >= 0 ? x : 0);
    }

    /// <summary>ロジスティックの釣り合い s_eq(pc) = G·pc·d/(D·d + G·pc)。d が +∞ なら線形の釣り合い G·pc/D（D が 0 以下なら +∞）。</summary>
    internal static double HabitLgEquilibrium(double g, double dcy, double d, double pc)
    {
        if (double.IsPositiveInfinity(d)) return dcy > 0 ? g * pc / dcy : double.PositiveInfinity;
        double den = dcy * d + g * pc;
        return den > 0 ? g * pc * d / den : 0;
    }

    /// <summary>実際の次の在庫 <paramref name="u"/> がロジスティックの釣り合いの上限を破るか（否定形＝NaN も破り）。上限＝max(s, s_eq)・率 G·pc/d + D が 1 を超える月は max(s, d)。</summary>
    internal static bool HabitLgViolates(double g, double dcy, double d, double s, double pc, double u, out double bound)
    {
        double rate = g * pc / d + dcy;
        bound = rate <= 1 ? Math.Max(s, HabitLgEquilibrium(g, dcy, d, pc)) : Math.Max(s, d);
        return !(u <= bound * (1.0 + 1e-9) + 1e-15);
    }

    static void HabitLgTrans(HabitlgAcc a, Coefficients c, double s, double pc, double u)
    {
        double g = c.OpiumAddictionGrowth, dcy = c.OpiumAddictionDecay, d = c.OpiumAddictionCap;
        var (o, l, raw) = HabitLgPair(g, dcy, d, s, pc);
        a.Trans++;
        if (BitConverter.DoubleToInt64Bits(u) != BitConverter.DoubleToInt64Bits(o)) a.OldMis++;
        if (BitConverter.DoubleToInt64Bits(u) != BitConverter.DoubleToInt64Bits(l)) a.LogMis++;
        if (raw > d) a.OverD++;
        a.OldLast = o; a.LogLast = l;
        if (u > a.StockMax) a.StockMax = u;
        if (HabitLgViolates(g, dcy, d, s, pc, u, out double bound))
        {
            a.Viol++;
            double over = u - bound;
            if (over > a.ViolMax) a.ViolMax = over;
        }
        if (u >= 0.995 * d) a.Pin++;
        if (c.OpiumLaborPenalty * u >= 0.5) a.Floor++;
    }

    /// <summary>州のその月の更新の前（<c>EconomyStep</c> の中毒度の更新の直前・読むだけ）：前の月の遷移を今の在庫で閉じ、今の月の前の在庫と pc を蓄える。</summary>
    internal static void HabitLgStep(Region r, Coefficients c, double perCapita)
    {
        var a = HabitlgAccs.GetValue(r, _ => new HabitlgAcc());
        double s = r.OpiumAddiction;
        if (a.HasPrev && a.PrevCoef is not null) HabitLgTrans(a, a.PrevCoef, a.PrevS, a.PrevPc, s);
        a.HasPrev = true; a.PrevS = s; a.PrevPc = perCapita; a.PrevCoef = c;
        a.Months++;
        if (s > 0 || perCapita > 0) a.Seen = true;
        a.PcSum += perCapita;
        if (perCapita < a.PcMin) a.PcMin = perCapita;
        if (perCapita > a.PcMax) a.PcMax = perCapita;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：最後の月の遷移を今の在庫で閉じ、蓄えた州の行を追記し、蓄えを捨てる。</summary>
    static void HabitlgTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = HabitlgSinks.GetValue(sim, _ => new HabitcapSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        if (!sk.Started) sb.Append(HabitlgHeaderText).Append('\n');
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            foreach (var r in w.Regions)
            {
                if (!HabitlgAccs.TryGetValue(r, out var a)) continue;
                if (a.HasPrev && a.PrevCoef is not null) HabitLgTrans(a, a.PrevCoef, a.PrevS, a.PrevPc, r.OpiumAddiction);
                if (a.Seen)
                    sb.Append(string.Join('	', q.ToString(inv), w.Year.ToString(inv), i.ToString(inv), NameOf(w), r.Name, a.Months.ToString(inv), HabitcapR(w.Coef.OpiumAddictionLogistic),
                        HabitcapR(a.Months > 0 ? a.PcSum / a.Months : 0), HabitcapR(a.Months > 0 ? a.PcMin : 0), HabitcapR(a.PcMax), HabitcapR(r.OpiumAddiction), HabitcapR(a.StockMax),
                        HabitcapR(a.OldLast), HabitcapR(a.LogLast), a.Trans.ToString(inv), a.OldMis.ToString(inv), a.LogMis.ToString(inv), a.Viol.ToString(inv), HabitcapR(a.ViolMax),
                        a.Pin.ToString(inv), a.Floor.ToString(inv), a.OverD.ToString(inv))).Append('\n');
                HabitlgAccs.Remove(r);
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"habitlg_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
