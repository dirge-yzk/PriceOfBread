namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (820) c65op アヘンの依存のロジスティック（形 H）の旧新の表 habitlg_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHabitcap.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。
// 目的＝旗 OpiumAddictionLogistic の旧（線形＋蓋）と新（s' = s + G·pc·(1 − s/d) − D·s）を同じ盤面の同じ前の在庫から並べ、実際の次の在庫がどちらに等しいかと、ロジスティックの釣り合いの不変式を数える。
// エンジンの関数（Habit.Of・Habit.NextStock）を呼ばない＝係数 G＝OpiumAddictionGrowth・D＝OpiumAddictionDecay・d＝OpiumAddictionCap（CapOff が正なら +∞）から自分で組む。
// 実際の次の在庫は計器が自分で読む＝次の月の口の時点の Region.OpiumAddiction と、四半期の末の表の時点の同じ欄（エンジンから受け取るのは 1 人あたり消費 pc だけ）。
// 表 habitlg_s（四半期の末・州の行・前の在庫か pc が正の月があった州だけ）：
//   monthsCount＝口が呼ばれた月数／logisticFlag＝旗の値／pcMeanPerMonth・pcMinPerMonth・pcMaxPerMonth＝その四半期の pc の平均・最小・最大／
//   stockLastIndex＝四半期の末の実際の在庫／stockMaxIndex＝その四半期の遷移の実際の次の在庫の最大／oldNextLastIndex・logNextLastIndex＝最後の遷移の旧・新の次の在庫（"R" 書式）／
//   transCount＝数えた遷移（月）／oldMismatchCount・logMismatchCount＝実際が旧・新とビットで違った遷移／
//   lgViolMonthsCount＝実際が max(s, s_eq(pc))（率 G·pc/d + D が 1 を超える月は max(s, d)）を相対 1e-9＋絶対 1e-15 を超えて破った遷移（s_eq = G·pc·d/(D·d + G·pc)・否定形で NaN も破り）／lgViolMaxIndex＝破った量の最大／
//   pinMonthsCount＝実際 ≧ 0.995·d／floorMonthsCount＝害×実際 ≧ 0.5（労働の床）／overDMonthsCount＝新の式の挟む前の値が d を超えた遷移（挟みが効いた月）。
// ⚠限界（自己申告）：助走の月は数えない（助走の明けの最初の月は前が無い）。pc は州の売れた量÷人口（POP 以外の買い手も含む＝(793) で 0）。
//   月が飛んだ州（その月の EconomyStep が呼ばれない）は飛んだ前後を 1 つの遷移として数える（稀）。旗 1 の腕では旧の列は〈同じ前の在庫に旧の式を当てた値〉であって旧の軌道ではない。
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
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, HabitcapSink> HabitlgSinks = new();

    /// <summary>旧（線形）と新（ロジスティック）の挟む前の値と挟んだ値。d は CapOff が正なら +∞。挟みは 0〜d・NaN は 0（エンジンの ClampOr と同じ規則を計器の側で書く）。</summary>
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
        double g = c.OpiumAddictionGrowth, dcy = c.OpiumAddictionDecay, d = c.OpiumAddictionCapOff > 0 ? double.PositiveInfinity : c.OpiumAddictionCap;
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
