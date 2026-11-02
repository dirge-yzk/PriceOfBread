namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (771) アヘンの蓋の旧新の表 habitcap_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandfix.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。
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
    sealed class HabitcapSink { public bool Started; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, HabitcapSink> HabitcapSinks = new();

    /// <summary>同じ前の在庫 <paramref name="stock"/> と 1 人あたり消費 <paramref name="perCapita"/> から、旧（蓋 <see cref="Coefficients.OpiumAddictionCap"/>）・新（蓋なし）・実際（<paramref name="h"/> の蓋）の次の在庫。</summary>
    internal static (double Old, double New, double Used) HabitCapPair(in HabitProfile h, Coefficients c, double stock, double perCapita)
        => (Habit.NextStock(h with { Cap = c.OpiumAddictionCap }, stock, perCapita),
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
                        LandfixR(w.Coef.OpiumAddictionCap), LandfixR(w.Coef.OpiumAddictionCapOff), LandfixR(a.Before), LandfixR(a.Used), LandfixR(a.Old), LandfixR(a.New), LandfixR(a.Excess), LandfixR(a.NewMax))).Append('\n');
                HabitcapAccs.Remove(r);
            }
        }
        System.IO.Directory.CreateDirectory(Dir);
        string path = System.IO.Path.Combine(Dir, $"habitcap_s{seed}.tsv");
        var enc = new System.Text.UTF8Encoding(false);
        if (!sk.Started) { System.IO.File.WriteAllText(path, sb.ToString(), enc); sk.Started = true; }
        else System.IO.File.AppendAllText(path, sb.ToString(), enc);
    }
}
