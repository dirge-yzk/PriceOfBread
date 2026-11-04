namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (771) アヘンの蓋の旧新の表 habitcap_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（(762) の InstrumentsLandfix.cs の型・(769) で削除）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。
// 目的＝旗 OpiumAddictionCapOff（蓋 0.2 を外す）の旧新を同じ盤面で読む。毎月の更新の前（同じ前の在庫）で、旧＝蓋 OpiumAddictionCap・新＝蓋なし(+∞)・実際＝いま使っている蓋 の 3 通りの次の在庫を並べる。
// 表 habitcap_s（四半期の末・州の行・前の在庫か 1 人あたり消費が正の月があった州だけ）：
//   monthsCount＝計器の口が呼ばれた月数／bindMonthsCount＝新が旧より大きい月（蓋が効いた月）／capIndex＝旧の蓋（係数）／capOffFlag＝旗の値／
//   stockBeforeLastIndex・usedNextLastIndex・oldNextLastIndex・newNextLastIndex＝四半期の最後の月の前の在庫と 3 通りの次の在庫（"R" 書式＝ビットで比べる）／
//   excessSumIndex＝Σ(新−旧)（蓋が外れたときに増える量）／newNextMaxIndex＝新の次の在庫の最大。
// ⚠限界（計器が自己申告する）：旧新は同じ前の在庫から 1 か月先だけを比べる＝旗 1 の腕では実際の在庫が蓋なしの軌道に乗るので、旧の列は〈同じ前の在庫に旧の蓋を当てた値〉であって旧の軌道ではない。
public static partial class Instruments
{
    internal const string HabitcapHeaderText = "quarter	year	nationIndex	nation	region	monthsCount	bindMonthsCount	capIndex	capOffFlag	stockBeforeLastIndex	usedNextLastIndex	oldNextLastIndex	newNextLastIndex	excessSumIndex	newNextMaxIndex";

    sealed class HabitcapAcc
    {
        public int Months, BindMonths; public bool Seen;
        public double Before, Used, Old, New, Excess, NewMax;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, HabitcapAcc> HabitcapAccs = new();
    static string HabitcapR(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);   // (773) 合流：c6bk が InstrumentsLandfix.cs（LandfixR の定義）を消したので同じ中身を局所に置く（文字の移し替えだけ）
    sealed class HabitcapSink { public bool Started; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, HabitcapSink> HabitcapSinks = new();

    /// <summary>同じ前の在庫 <paramref name="stock"/> と 1 人あたり消費 <paramref name="perCapita"/> から、旧（蓋 <see cref="Coefficients.OpiumAddictionCap"/>）・新（蓋なし）・実際（<paramref name="h"/> の蓋）の次の在庫。</summary>
    internal static (double Old, double New, double Used) HabitCapPair(in HabitProfile h, Coefficients c, double stock, double perCapita)
        => (Habit.NextStock(h with { Cap = c.OpiumAddictionCap, Logistic = false }, stock, perCapita),   // (820) 旧＝線形＋蓋を明示（旗 OpiumAddictionLogistic が 0 なら h の Logistic は偽＝今と同じ値）
            Habit.NextStock(h with { Cap = double.PositiveInfinity }, stock, perCapita),
            Habit.NextStock(h, stock, perCapita));

    /// <summary>州のその月の更新の前（<c>EconomyStep</c> の中毒度の更新の直前・読むだけ）：旧新の次の在庫を蓄える。</summary>
    internal static void HabitCapStep(Region r, Coefficients c, in HabitProfile h, double stock, double perCapita)
    {
        var (o, n, u) = HabitCapPair(h, c, stock, perCapita);
        var a = HabitcapAccs.GetValue(r, _ => new HabitcapAcc());
        a.Months++;
        if (n > o) a.BindMonths++;
        if (stock > 0 || perCapita > 0) a.Seen = true;
        a.Before = stock; a.Used = u; a.Old = o; a.New = n;
        a.Excess += n - o;
        if (n > a.NewMax) a.NewMax = n;
        HabitBalStep(r, c, stock, perCapita, u);   // (787) 釣り合いの表の蓄え（係数だけから組み立てる＝Habit.Of を呼ばない）
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：蓄えた州の行を追記し、蓄えを 0 へ戻す。</summary>
    static void HabitcapTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = HabitcapSinks.GetValue(sim, _ => new HabitcapSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        if (!sk.Started) sb.Append(HabitcapHeaderText).Append('\n');
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            foreach (var r in w.Regions)
            {
                if (!HabitcapAccs.TryGetValue(r, out var a)) continue;
                if (a.Seen)
                    sb.Append(string.Join('	', q.ToString(inv), w.Year.ToString(inv), i.ToString(inv), NameOf(w), r.Name, a.Months.ToString(inv), a.BindMonths.ToString(inv),
                        HabitcapR(w.Coef.OpiumAddictionCap), HabitcapR(w.Coef.OpiumAddictionCapOff), HabitcapR(a.Before), HabitcapR(a.Used), HabitcapR(a.Old), HabitcapR(a.New), HabitcapR(a.Excess), HabitcapR(a.NewMax))).Append('\n');
                HabitcapAccs.Remove(r);
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"habitcap_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }

    // ---------------------------------------------------------------- (787) c9op アヘンの蓋の段2：釣り合いの表 habitbal_s（読むだけ・恒等） ----------------------------------------------------------------
    // 目的＝蓋を外したときの上限の門を〈式から書いた釣り合い〉で読む。1 か月の更新 s' = s + G·pc − D·s（G＝成長・D＝減衰・pc＝1 人あたり消費）は、s ≦ (G/D)·pc なら s' ≦ (G/D)·pc、s がそれを超えていれば s' < s
    //   ＝ s' ≦ max(s, (G/D)·pc) が蓋なしでも毎月成り立つ（減衰を壊した実装・成長を過大にした実装は破れる）。釣り合いの値は係数 OpiumAddictionGrowth・OpiumAddictionDecay から直に組む（Habit.Of・Habit.NextStock の式は使わない・実際の次の在庫 u だけを読む）。
    // 表 habitbal_s（四半期の末・州の行・habitcap_s と同じ条件の州だけ）：
    //   monthsCount＝口が呼ばれた月数／pcMaxPerMonth＝その四半期の 1 人あたり消費の最大（毎月）／stockBeforeMaxIndex・stockUsedMaxIndex＝前の在庫・実際の次の在庫の最大／
    //   balViolMonthsCount＝s' が max(s, (G/D)·pc) を（相対 1e-9＋絶対 1e-15 を超えて）破った月数／balViolMaxIndex＝破った量の最大／floorMonthsCount＝実際の次の在庫で労働の床が効く月（害×在庫 ≧ 0.5）。
    // ⚠限界（自己申告）：助走の月は数えない。1 人あたり消費 pc は州の売れた量÷州の人口で、人口の消費だけでなく工場の投入ほかを含む（産地・交易の州では高く出る＝依存の高さが人の消費の多さを意味しない）。
    internal const string HabitbalHeaderText = "quarter	year	nationIndex	nation	region	monthsCount	pcMaxPerMonth	stockBeforeMaxIndex	stockUsedMaxIndex	balViolMonthsCount	balViolMaxIndex	floorMonthsCount";

    sealed class HabitbalAcc
    {
        public int Months, ViolMonths, FloorMonths; public bool Seen;
        public double PcMax, BeforeMax, UsedMax, ViolMax;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, HabitbalAcc> HabitbalAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, HabitcapSink> HabitbalSinks = new();

    /// <summary>釣り合いの上限 max(s, (G/D)·pc)。減衰が 0 以下なら上限なし(+∞)。係数だけから組む。</summary>
    internal static double HabitBalCeiling(double growth, double decay, double stock, double perCapita)
        => Math.Max(stock, decay > 0 ? growth / decay * perCapita : double.PositiveInfinity);

    /// <summary>次の在庫 <paramref name="used"/> が釣り合いの上限を破るか（否定形＝NaN も破りとして数える）。</summary>
    internal static bool HabitBalViolates(double growth, double decay, double stock, double perCapita, double used)
    {
        double ceil = HabitBalCeiling(growth, decay, stock, perCapita);
        return !(used <= ceil * (1.0 + 1e-9) + 1e-15);
    }

    static void HabitBalStep(Region r, Coefficients c, double stock, double perCapita, double used)
    {
        var a = HabitbalAccs.GetValue(r, _ => new HabitbalAcc());
        a.Months++;
        if (stock > 0 || perCapita > 0) a.Seen = true;
        if (perCapita > a.PcMax) a.PcMax = perCapita;
        if (stock > a.BeforeMax) a.BeforeMax = stock;
        if (used > a.UsedMax) a.UsedMax = used;
        if (HabitBalViolates(c.OpiumAddictionGrowth, c.OpiumAddictionDecay, stock, perCapita, used))
        {
            a.ViolMonths++;
            double over = used - HabitBalCeiling(c.OpiumAddictionGrowth, c.OpiumAddictionDecay, stock, perCapita);
            if (over > a.ViolMax) a.ViolMax = over;
        }
        if (c.OpiumLaborPenalty * used >= 0.5) a.FloorMonths++;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：蓄えた州の行を追記し、蓄えを 0 へ戻す。</summary>
    static void HabitbalTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = HabitbalSinks.GetValue(sim, _ => new HabitcapSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        if (!sk.Started) sb.Append(HabitbalHeaderText).Append('\n');
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            foreach (var r in w.Regions)
            {
                if (!HabitbalAccs.TryGetValue(r, out var a)) continue;
                if (a.Seen)
                    sb.Append(string.Join('	', q.ToString(inv), w.Year.ToString(inv), i.ToString(inv), NameOf(w), r.Name, a.Months.ToString(inv),
                        HabitcapR(a.PcMax), HabitcapR(a.BeforeMax), HabitcapR(a.UsedMax), a.ViolMonths.ToString(inv), HabitcapR(a.ViolMax), a.FloorMonths.ToString(inv))).Append('\n');
                HabitbalAccs.Remove(r);
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"habitbal_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
