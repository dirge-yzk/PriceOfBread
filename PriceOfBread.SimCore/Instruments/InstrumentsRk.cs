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

    // ---- 0c：届き ρ の純関数 ----
    /// <summary>届き ρ＝1÷(1＋実効の km÷DistanceNormKm)。実効の km＝km×(1−覆い×(1−線路の費用の比))・覆い＝線路の被覆 φ（RailPhi）×その主体の取り分（どちらも 0〜1 で切る）。
    /// km は線路なしの距離を渡す＝線路の効きはこの関数の中で 1 回だけ数える。使う既存の量＝RailPhi（Transport）・RailCostFraction（BilateralMarket の陸の費用の比と同じ係数）・DistanceNormKm。
    /// 費用（km）↑で ρ↓・鉄道×取り分↑で ρ↑（線路の費用の比＜1 のとき）。</summary>
    internal static double ReachRho(double km, double railPhi, double share, double railCostFraction)
    {
        double cover = railPhi > 0 && share > 0 ? Math.Min(1.0, railPhi) * Math.Min(1.0, share) : 0.0;
        double fr = railCostFraction > 0 ? Math.Min(1.0, railCostFraction) : 0.0;
        return RulePowerReach(km * (1.0 - cover * (1.0 - fr)));
    }

    internal const string ReachHeaderText = "quarter\tyear\tnation\tregion\tnoRailKm\trailTrack\trailNeed\trailPhi\townerShare\trhoNoRail\trhoOwner";
    internal static long ReachRowCount;   // 純カウンタ（番人が〈計器が切れている時は増えない〉を数える）
    sealed class ReachSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, ReachSink> ReachSinks = new();

    /// <summary>四半期の末：挙げた国は毎四半期・全ての国は年末（q%4==3）に、首都から各州への届きの行（持ち主の取り分で数えた分）。四半期の最初の清算の複写（RpSnap）が要る。</summary>
    static void ReachTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("reach_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        if (RpSnapOf(sim.Diplomacy) is not { } snap) return;
        var rs = ReachSinks.GetValue(sim, _ => new ReachSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0 || !(q % 4 == 3 || IsPicked(w, S(w)))) continue;
            var cap = w.Regions[0];
            if (!snap.Idx.TryGetValue(cap, out int capIx)) continue;
            var c = w.Coef; double target = Transport.Target(w, c), totPop = Math.Max(0, w.TotalPopulation);
            double[]? noRail = null;
            foreach (var r in w.Regions)
            {
                if (ReferenceEquals(r, cap) || !snap.Idx.TryGetValue(r, out int di)) continue;
                if (noRail is null) { noRail = new double[snap.Tc]; System.Array.Fill(noRail, c.LandSeaCostRatio); }
                if (rs.W is null) { rs.W = Open("reach", seed); rs.W.WriteLine(ReachHeaderText); }
                double km = snap.Get(capIx, di, noRail);
                double need = target * (totPop > 0 ? Math.Max(0, r.SumPopulation()) / totPop : 0.0);
                double phi = Transport.RailPhi(r.RailTrack, need);
                double own = r.Rule.Length > 0 ? r.Rule[0] : 0.0;
                rs.W.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), r.Name, F(km), F(r.RailTrack), F(need), F(phi), F(own),
                    F(RulePowerReach(km)), F(ReachRho(km, phi, own, c.RailCostFraction))));
                ReachRowCount++;
            }
        }
    }

    static void ReachClose(MultiNationSim sim) { if (ReachSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }

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
