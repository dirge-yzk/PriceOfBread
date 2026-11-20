using System.Runtime.CompilerServices;

namespace PriceOfBread.SimCore;

// (1261) 州の人口の閉じ＝州×四半期で ΔP＝記帳した流入出＋出生−死亡 を並べ、残差を読む計器（popclose_s）。
// 力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない（蓄えは Region を鍵の弱い表＝セーブに載らない）。
// 流れの量は自分の hook で積む（region_s の FlowOf は読まない＝region_s が選ばれなくても同じ列が出る）。
public static partial class Instruments
{
    /// <summary>州の人口を動かす口の種類（popclose_s の列）。兵の移し（Garrison）は <see cref="PopMove"/> を Military 側からも呼べる（gs の前提＝波130）。</summary>
    internal enum PopMoveKind { Migration = 0, CrossBorder = 1, Specialist = 2, Garrison = 3, ColonyMerge = 4 }

    internal const string PopCloseHeaderText = "quarter\tyear\tnation\tregion\ttier\tfresh\tprevPopulationK\tpopulationK\tdeltaPopulationK"
        + "\tmigrationInK\tmigrationOutK\tcrossBorderInK\tcrossBorderOutK\tspecialistInK\tspecialistOutK\tgarrisonInK\tgarrisonOutK\tcolonyMergeInK"
        + "\tbirthsK\tdeathsNormalK\tdeathsFamineK\tresidualK\tresidualPerPrev";

    internal sealed class PopCloseAcc
    {
        public double Prev = double.NaN;
        public readonly double[] In = new double[5];
        public readonly double[] Out = new double[5];
        public double Births, DeathsNormal, DeathsFamine;
        public void Clear() { Array.Clear(In); Array.Clear(Out); Births = DeathsNormal = DeathsFamine = 0; }
    }
    static readonly ConditionalWeakTable<Region, PopCloseAcc> PopCloseAccs = new();
    static PopCloseAcc PopCloseOf(Region r) => PopCloseAccs.GetValue(r, _ => new PopCloseAcc());

    /// <summary>州の人口の移し 1 件（読むだけ）：src の州から dst の州へ k（千人）。片方が州でない（器から入る・州の外へ出る）ときは null。</summary>
    internal static void PopMove(PopMoveKind kind, Region? src, Region? dst, double k)
    {
        if (!Tg.PopClose) return;
        if (!(k > 0)) return;   // 否定形＝NaN も弾く
        if (src is not null) PopCloseOf(src).Out[(int)kind] += k;
        if (dst is not null) PopCloseOf(dst).In[(int)kind] += k;
    }

    /// <summary>出生と死亡（<see cref="RdhDemo"/> の頭から・DemographicStep の州のループの中）。</summary>
    static void PopCloseDemo(Region r, double births, double deathsNormal, double deathsFamine)
    {
        var a = PopCloseOf(r); a.Births += births; a.DeathsNormal += deathsNormal; a.DeathsFamine += deathsFamine;
    }

    static double PopCloseSize(Region r) { double s = 0; foreach (var p in r.Pops) s += p.Size; return s; }

    sealed class PopCloseSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly ConditionalWeakTable<MultiNationSim, PopCloseSink> PopCloseSinks = new();

    /// <summary>助走の直後（<see cref="Phase"/> の warm）：全ての州の前の人口を今の値にし、助走の間の積みを空にする。</summary>
    static void PopClosePhase(MultiNationSim sim, string time)
    {
        if (!Tg.PopClose || time != "warm") return;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) { var a = PopCloseOf(r); a.Prev = PopCloseSize(r); a.Clear(); }
    }

    /// <summary>四半期の末：州ごとの行（前の四半期の末が無い州＝fresh は ΔP と残差が nan）。</summary>
    static void PopCloseTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.PopClose) return;
        var sk = PopCloseSinks.GetValue(sim, _ => new PopCloseSink());
        sk.W ??= Open("popclose", seed);
        if (!sk.Header) { sk.W.WriteLine(PopCloseHeaderText); sk.Header = true; }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string I(int v) => v.ToString(inv);
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nation = NameOf(w);
            foreach (var r in w.Regions)
            {
                var a = PopCloseOf(r);
                double pop = PopCloseSize(r);
                bool fresh = double.IsNaN(a.Prev);
                double inSum = 0, outSum = 0;
                for (int k = 0; k < a.In.Length; k++) { inSum += a.In[k]; outSum += a.Out[k]; }
                double delta = fresh ? double.NaN : pop - a.Prev;
                double resid = fresh ? double.NaN : delta - (inSum - outSum + a.Births - a.DeathsNormal - a.DeathsFamine);
                double rate = fresh || !(a.Prev > 1e-9) ? double.NaN : Math.Abs(resid) / a.Prev;
                sk.W.WriteLine(string.Join('\t', I(q), I(w.Year), nation, r.Name, r.Tier.ToString(), fresh ? "1" : "0", F(a.Prev), F(pop), F(delta),
                    F(a.In[0]), F(a.Out[0]), F(a.In[1]), F(a.Out[1]), F(a.In[2]), F(a.Out[2]), F(a.In[3]), F(a.Out[3]), F(a.In[4]),
                    F(a.Births), F(a.DeathsNormal), F(a.DeathsFamine), F(resid), F(rate)));
                a.Prev = pop; a.Clear();
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PopCloseClose(MultiNationSim sim)
    {
        if (!Tg.PopClose) return;
        if (PopCloseSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
