namespace PriceOfBread.SimCore;

// (1130) c110lo：鉄道のインフラの払いの計器 railspend_s（国×四半期）。事前登録 measurements/m1130_20261114_prereg_lo.txt。
//   投資家が決済で実際に買った線路と機関車（建設に付く分・保守の分）の量と額（清算の値段×量×FxRate＝金建て）を、
//   払いの時に州の Discovered の機関車の行が偽か真か（pre／post）で分けて足す。行の番号は計器が Catalog から自分で引く（Discovery.RailTransport を呼ばない）。
//   力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。助走（WarmupFrozen）の月は数えない。
public static partial class Instruments
{
    internal const string RailSpendHeaderText = "year\tq\tnation\tdiscEnd\tmonths\tmonthsPre\t"
        + "preBuildRailQty\tpreBuildRailAmtGold\tpreBuildLocoQty\tpreBuildLocoAmtGold\tpreMaintRailQty\tpreMaintRailAmtGold\tpreMaintLocoQty\tpreMaintLocoAmtGold\t"
        + "postBuildRailQty\tpostBuildRailAmtGold\tpostBuildLocoQty\tpostBuildLocoAmtGold\tpostMaintRailQty\tpostMaintRailAmtGold\tpostMaintLocoQty\tpostMaintLocoAmtGold\t"
        + "monthsWorldOnly\tunknownGovRailAddCap";
    // (1152) c113lc の 2 列（事前登録 measurements/m1152_20261115_prereg_lc.txt）：
    //   monthsWorldOnly＝月の締めで国の Discovered の機関車の行が偽・World.WorldDiscovered の同じ行が真の月の数（計器が両方の配列を自分で読む）。
    //   unknownGovRailAddCap＝州の Discovered の機関車の行が偽の州×月の、輸送の段（Transport.Step）の線路の敷きの量（容量の単位）。
    //     計器が自分で州の線路の在庫を前の月の締めから控え、今の在庫−前の在庫×(1−TransportDecay) の正の分を足す（エンジンの局所を受け取らない・
    //     戦災の減りは負＝足さない・国を移った州は移った先の最初の月を数えない）。旗 RailByRegion＞0 の道だけ（0 の道は州の在庫を使わない＝0）。

    /// <summary>計器が自分で引く機関車の発見の行（Catalog の発見の表の〈機関車〉の行）。</summary>
    static readonly int RsLocoRow = Catalog.DiscoveryRowOf(Catalog.LocomotiveId, "");
    static bool RsKnown(bool[]? d) => d is not null && RsLocoRow >= 0 && RsLocoRow < d.Length && d[RsLocoRow];

    sealed class RsAcc { public int Months, MonthsPre, MonthsWorldOnly; public double UnknownGovRailAdd; public readonly double[] V = new double[16]; public readonly System.Collections.Generic.Dictionary<Region, double> PrevTrack = new(); }
    sealed class RsSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, RsAcc> RsAccs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, RsSink> RsSinks = new();

    /// <summary>決済の直後に呼ぶ：maint＝保守の決済（偽は建設に付く分）。量は実際に移った量・値段はその月の清算の値段。</summary>
    internal static void RailSpendAdd(World w, Region r, bool maint, double railQty, double railPrice, double locoQty, double locoPrice)
    {
        if (w.WarmupFrozen || !Wants("railspend_s")) return;
        var a = RsAccs.GetValue(w, _ => new RsAcc());
        int b = (RsKnown(r.Discovered) ? 8 : 0) + (maint ? 4 : 0);
        double fx = w.FxRate;
        a.V[b] += railQty; a.V[b + 1] += railQty * railPrice * fx;
        a.V[b + 2] += locoQty; a.V[b + 3] += locoQty * locoPrice * fx;
    }

    /// <summary>月の締め（MonthClose）：月の数と、国が機関車を知らない月の数。</summary>
    static void RailSpendMonth(World w)
    {
        if (w.WarmupFrozen || !Wants("railspend_s")) return;
        var a = RsAccs.GetValue(w, _ => new RsAcc());
        a.Months++; if (!RsKnown(w.Discovered)) a.MonthsPre++;
        if (!RsKnown(w.Discovered) && RsKnown(w.WorldDiscovered)) a.MonthsWorldOnly++;
        if (!(w.Coef.RailByRegion > 0)) return;
        double keep = 1 - w.Coef.TransportDecay;
        var seen = new System.Collections.Generic.HashSet<Region>();
        foreach (var r in w.Regions)
        {
            seen.Add(r);
            double t = r.RailTrack;
            if (a.PrevTrack.TryGetValue(r, out double prev) && !RsKnown(r.Discovered))
            {
                double add = t - System.Math.Max(0, prev * keep);
                if (add > 1e-12 * System.Math.Max(1.0, t)) a.UnknownGovRailAdd += add;
            }
            a.PrevTrack[r] = t;
        }
        if (a.PrevTrack.Count > seen.Count)
        {
            var gone = new System.Collections.Generic.List<Region>();
            foreach (var r in a.PrevTrack.Keys) if (!seen.Contains(r)) gone.Add(r);
            foreach (var r in gone) a.PrevTrack.Remove(r);
        }
    }

    static void RailSpendTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("railspend_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = RsSinks.GetValue(sim, _ => new RsSink());
        if (sk.W is null) { sk.W = Open("railspend", seed); sk.W.WriteLine(RailSpendHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int yearLabel = sim.Nations[0].World.Year;
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var a = RsAccs.GetValue(w, _ => new RsAcc());
            sb.Clear();
            sb.Append(I(yearLabel)).Append('\t').Append(I(q)).Append('\t').Append(NameOf(w)).Append('\t').Append(RsKnown(w.Discovered) ? '1' : '0')
              .Append('\t').Append(I(a.Months)).Append('\t').Append(I(a.MonthsPre));
            foreach (double v in a.V) sb.Append('\t').Append(FR(v));
            sb.Append('\t').Append(I(a.MonthsWorldOnly)).Append('\t').Append(FR(a.UnknownGovRailAdd));
            sk.W.WriteLine(sb.ToString());
            a.Months = 0; a.MonthsPre = 0; a.MonthsWorldOnly = 0; a.UnknownGovRailAdd = 0; System.Array.Clear(a.V);
        }
    }

    static void RailSpendClose(MultiNationSim sim) { if (RsSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
