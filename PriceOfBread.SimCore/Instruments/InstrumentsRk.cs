namespace PriceOfBread.SimCore;

// (1235) c125rk 計器（支配の力の段 E・P・F・G の前の恒等の線）：0a 外国の請求の列・0c 届き ρ の純関数と四半期の表・講和の押しの列。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable）。計器が切れている間（!On）は何も計算しない。
public static partial class Instruments
{
    // ---- 0a：国の表 nation_s の末尾に足す 5 列（displayName の後ろ） ----
    internal const string NationRkHeaderText = "foreignDebt\tforeignCapitalIn\tforeignBondClaims\tcapitalAbroad\te_n";

    /// <summary>外国の請求の比 e_n＝F÷(F＋年間の名目 GDP)・F＝外国人が持つ請求（公債の外国の分＋この国にある外資）。0〜1 の比で、F が正でない・非有限・GDP が負なら 0（否定形）。
    /// 分母の物差しは AI の案（確証なし）＝段 P の e の定義は 0a の実測の後に確かめる。</summary>
    internal static double ForeignClaimShare(double foreignDebt, double foreignCapitalIn, double nominalGdpPerYear)
    {
        double f = foreignDebt + foreignCapitalIn;
        if (!(f > 0) || !double.IsFinite(f) || !(nominalGdpPerYear >= 0) || !double.IsFinite(nominalGdpPerYear)) return 0.0;
        return f / (f + nominalGdpPerYear);
    }

    /// <summary>nation_s の行の末尾へ足す 5 列（見出しは <see cref="NationRkHeaderText"/>）。読むだけ。</summary>
    static void NationRkCols(World w, List<string> cols)
    {
        double fd = w.ForeignDebt.Value, fci = w.ForeignCapitalIn.Value;
        cols.Add(F(fd)); cols.Add(F(fci)); cols.Add(F(w.ForeignBondClaims.Value)); cols.Add(F(w.CapitalAbroad.Value));
        cols.Add(F(ForeignClaimShare(fd, fci, Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value)));
    }

    // ---- 0c：届き ρ ----
    /// <summary>州の線路なしの費用（馬の費用）＝清算が terrCost に入れる式の bh（BilateralMarket.cs の RailByRegion の枝と同じ＝地形の費用・大河と小川は上書き）。線路の効き（phi）を含まない。</summary>
    internal static double HorseCostOf(Region r, Coefficients c)
    {
        int ti = (int)r.Terrain;
        double bh = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
        if (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) bh = c.TerrainWaterMajorCost;
        else if (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) bh = c.TerrainWaterMinorCost;
        return bh;
    }

    /// <summary>二つの州の間の距離 km を 2 通り返す（読むだけ）。Owner＝清算が組んだままの terrCost・陸の比で 1 回（線路の効きは terrCost に入っている＝ここで重ねない）。
    /// NoRail＝両端の terrCost を線路なしの費用（HorseCostOf）へ、陸の比を LandSeaCostRatio（<paramref name="landNoRail"/>）へ差し替えた距離（線路の伸びに動かない）。差し替えは呼びの間だけ（戻す）。</summary>
    internal static (double NoRail, double Owner) ReachKm(RpSnap snap, int a, int b, Coefficients c, double[] landNoRail)
    {
        double owner = snap.Get(a, b);
        double ta = snap.TerrCost[a], tb = snap.TerrCost[b];
        double noRail;
        try
        {
            snap.TerrCost[a] = HorseCostOf(snap.Tr[a].r, c); snap.TerrCost[b] = HorseCostOf(snap.Tr[b].r, c);
            noRail = snap.Get(a, b, landNoRail);
        }
        finally { snap.TerrCost[a] = ta; snap.TerrCost[b] = tb; }
        return (noRail, owner);
    }

    /// <summary>講和・離反の投影と同じ形（Politics.cs の 1÷(1＋DistanceWarDecay×km÷DistanceNormKm)）。km が非有限・負なら 0。</summary>
    internal static double WarDecayReach(double km, double distanceWarDecay)
        => double.IsFinite(km) && km >= 0 && double.IsFinite(distanceWarDecay) ? 1.0 / (1.0 + distanceWarDecay * km / Diplomacy.DistanceNormKm) : 0.0;

    /// <summary>鉄道の need の州の取り分の基底（清算と同じ Transport.RailNeedShare に渡す）。旗 1 は州の産出の控え（Region.GdpOutputMemo）の和＝GdpPerMonth を読まない（計算量の台帳を増やさない）。</summary>
    static (double Even, double TotalPot) RailBasisNoScan(World w, Coefficients c)
    {
        if (!(c.RailNeedByOutput > 0)) return Transport.RailNeedBasis(w, c);
        double tot = 0; foreach (var r in w.Regions) tot += r.GdpOutputMemo;
        return (w.Regions.Count > 0 ? 1.0 / w.Regions.Count : 1.0, tot);
    }

    internal const string ReachHeaderText = "quarter\tyear\tnation\tregion\tnoRailKm\trailTrack\trailNeed\trailPhi\townerShare\trhoNoRail\trhoOwner\townerKm\trhoWarDecay";
    internal static long ReachRowCount;   // 純カウンタ（番人が〈計器が切れている時は増えない〉を数える）
    internal static long ReachTicks;      // reach_s の実時間（Stopwatch の刻み・.cost の項の測り）
    sealed class ReachSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ReachSink> ReachSinks = new();

    /// <summary>四半期の末：挙げた国は毎四半期・全ての国は年末（q%4==3）に、首都から各州への届きの行（持ち主の取り分で数えた分）。四半期の最初の清算の複写（RpSnap）が要る。</summary>
    static void ReachTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("reach_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        if (RpSnapOf(sim.Diplomacy) is not { } snap) return;
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var rs = ReachSinks.GetValue(sim, _ => new ReachSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0 || !(q % 4 == 3 || IsPicked(w, S(w)))) continue;
            var cap = w.Regions[0];
            if (!snap.Idx.TryGetValue(cap, out int capIx)) continue;
            var c = w.Coef; double target = Transport.Target(w, c);
            var basis = RailBasisNoScan(w, c);
            double[]? landNoRail = null;
            foreach (var r in w.Regions)
            {
                if (ReferenceEquals(r, cap) || !snap.Idx.TryGetValue(r, out int di)) continue;
                if (landNoRail is null) { landNoRail = new double[snap.Tc]; System.Array.Fill(landNoRail, c.LandSeaCostRatio); }
                if (rs.W is null) { rs.W = Open("reach", seed); rs.W.WriteLine(ReachHeaderText); }
                var (kmNo, kmOwn) = ReachKm(snap, capIx, di, c, landNoRail);
                double need = target * Transport.RailNeedShare(w, basis, r);
                double phi = Transport.RailPhi(r.RailTrack, need);
                double own = r.Rule.Length > 0 ? r.Rule[0] : 0.0;
                rs.W.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), r.Name, F(kmNo), F(r.RailTrack), F(need), F(phi), F(own),
                    F(RulePowerReach(kmNo)), F(RulePowerReach(kmOwn)), F(kmOwn), F(WarDecayReach(kmOwn, c.DistanceWarDecay))));
                ReachRowCount++;
            }
        }
        ReachTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
    }

    static void ReachClose(MultiNationSim sim)
    {
        if (!ReachSinks.TryGetValue(sim, out var s)) return;
        if (s.W is not null) System.Console.Error.WriteLine($"[reach_s] ms={ReachTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F1} rows={ReachRowCount}");
        s.W?.Dispose(); s.W = null;
    }

    // ---- 講和の押し：終戦の記録（EndedWarRecord）に押しの欄は無い＝終戦の 1 本（Diplomacy.EndWar）が On のときだけ計器の置き場へ写す ----
    sealed class PushBox { public double A, B; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EndedWarRecord, PushBox> WarPushes = new();

    /// <summary>終戦の 1 本の記録の追記の直後（Instruments.On のときだけ）：その戦争の積んだ押し（毎月 war.PushA・PushB に足されたもの）を、いま追記した記録へ紐づける。</summary>
    internal static void WarEndPush(Diplomacy d, War war)
    {
        if (d.EndedWars.Count == 0) return;
        WarPushes.AddOrUpdate(d.EndedWars[^1], new PushBox { A = war.PushA, B = war.PushB });
    }

    /// <summary>講和の行の末尾 2 列（勝者の側の押し・敗者の側の押し）。記録が写されていなければ nan。</summary>
    internal static (double Winner, double Loser) WarPushOf(EndedWarRecord e)
        => WarPushes.TryGetValue(e, out var b) ? (e.Score > 0 ? (b.A, b.B) : (b.B, b.A)) : (double.NaN, double.NaN);
}
