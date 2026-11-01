namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (748) 入口の形の表 migform_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsIdn.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（積みは計器の側の欄）。
// 目的＝事前登録 measurements/m748_20261101_prereg_hol.txt の M1（遠方の計画の新÷旧）・M2（生の差が正で距離の項を引いて負になる組の割合）を同じ盤面で読む。
// 行＝国×四半期（国内の移民の計画が 1 組でも有った国だけ）。Territory.Step の出る州ごとに <see cref="MigformObserve"/> が積み、四半期の末に書いて 0 へ戻す。
//   rawPosPairs＝生の差（機会−残る値打ち−摩擦）が正の組の数／livePosPairs＝今の規則（旗 MigrationEntryFriction に従う）でも差が正の組の数／
//   liveFarPlanK・liveAllPlanK＝今の規則の計画の人数（割合×動ける人数・千人）のうち 3,000 km 超の行き先・全ての行き先／oldFarPlanK・oldAllPlanK＝同じ盤面で旧の規則（生の差×MigrationDecay）の計画。
//   旗 0 では live と old が同じ式＝ビット一致（M1＝1・M2＝0）。計画は受け入れの上限で縮める前（本体の plan）の量。
// ⚠限界（計器が自己申告する）：旧の規則の計画は同じ月の盤面から数えた反実仮想（旗 1 の軌道では旧の規則の世界は走らない）／3,000 km は大圏（Geo.DistanceKm）。
public static partial class Instruments
{
    internal const string MigformHeaderText = "quarter	year	nation	rawPosPairs	livePosPairs	liveFarPlanK	liveAllPlanK	oldFarPlanK	oldAllPlanK";

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> MigformAcc = new();
    sealed class MigformSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, MigformSink> MigformSinks = new();

    /// <summary><see cref="Territory.Step"/> の出る州ごと（計器が on のときだけ）：この国のこの月の積みへ足す。</summary>
    internal static void MigformObserve(World w, long rawPos, long livePos, double liveFar, double liveAll, double oldFar, double oldAll)
    {
        var a = MigformAcc.GetValue(w, _ => new double[6]);
        a[0] += rawPos; a[1] += livePos; a[2] += liveFar; a[3] += liveAll; a[4] += oldFar; a[5] += oldAll;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた積みを国ごとに書いて 0 へ戻す。</summary>
    static void MigformTable(MultiNationSim sim, ulong seed, int q)
    {
        foreach (var n in sim.Nations)
        {
            if (!MigformAcc.TryGetValue(n.World, out var a) || !(a[0] > 0)) continue;
            var sk = MigformSinks.GetValue(sim, _ => new MigformSink());
            sk.W ??= Open("migform", seed);
            if (!sk.Header) { sk.W.WriteLine(MigformHeaderText); sk.Header = true; }
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            sk.W.WriteLine(string.Join('	', q.ToString(inv), n.World.Year.ToString(inv), NameOf(n.World), F(a[0]), F(a[1]), F(a[2]), F(a[3]), F(a[4]), F(a[5])));
            Array.Clear(a);
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void MigformClose(MultiNationSim sim)
    {
        if (MigformSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
