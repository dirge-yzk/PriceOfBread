namespace PriceOfBread.SimCore;

// (1323) c138lp 計器 lawdir_s：法の好みの向き（拡大と後退）の純支持を、旧の規則（後退＝今の段の 1 段上げの好みの反転）と
// 新の規則（後退＝1 段下の段からの 1 段上げの好みの反転＝単峰の一貫性）で同じ盤面に並べ、エンジンが実際に使う純支持（公開の NetSupport）と同じ行に書く。
// 段に依る Pref の項（Suffrage の近代エリート・Inheritance の段の大きさ・MonetaryStandard の銀）は事前登録 measurements/m1323_20261123_prereg_lp.txt の
// 追記 2 の文から計器が自分で式を写し、段 k と段 k−1 で評価する（エンジンの Pref の back を通さない）。他の軸は Pref(…,向き) を入力として読む。
// 計器の国の年末だけ・軸ごとに 1 行。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（Build は窓の外で呼ぶ）。
public static partial class Instruments
{
    internal const string LawDirHeaderText = "quarter\tyear\tnation\taxis\tlevelShare\tprefTradIndex\tprefModernIndex\tprefMassIndex"
        + "\tnetUpOldIndex\tnetDownOldIndex\tnetUpNewIndex\tnetDownNewIndex\tnetUpUsedIndex\tnetDownUsedIndex\tsinglePeakedFlag";

    sealed class LdSink { public System.IO.StreamWriter? W; public readonly List<string> Peek = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LdSink> LdSinks = new();

    /// <summary>試験の読み口：直前に書いた年末の行。</summary>
    internal static IReadOnlyList<string> LawDirPeek(MultiNationSim sim) => LdSinks.GetValue(sim, _ => new LdSink()).Peek;

    /// <summary>段に依る 3 軸の 1 段上げの好み（計器の写し）。shift＝1 なら 1 段下の段で読む。他の軸は NaN（呼び手が Pref を読む）。</summary>
    static double LdLevelPref(World w, PolicyAxis a, int b, in PolicyPreferences.Profile e, int shift)
    {
        bool mass = b == 2, modern = b == 1, trad = b == 0;
        switch (a)
        {
            case PolicyAxis.Suffrage:
            {
                double mil = 0, size = 0;
                foreach (var p in w.AllPops()) if (Politics.IsMassPop(p)) { mil += p.Militancy * p.Size; size += p.Size; }
                double t = (!(size > 1e-9) ? 0 : mil / size) * w.Coef.SuffrageConcession;
                int lvl = System.Math.Max(0, (int)w.Suffrage - shift);
                return trad ? -1.0 + t : modern ? (lvl < (int)Suffrage.Census ? 1.0 : -1.0 + t) : 1.0;
            }
            case PolicyAxis.Inheritance:
            {
                int cur = System.Math.Max(0, (int)w.Inheritance - shift), top = (int)InheritanceLaw.All;
                double step = cur < top ? Government.InheritanceRate((InheritanceLaw)(cur + 1)) - Government.InheritanceRate((InheritanceLaw)cur)
                                        : Government.InheritanceRate(InheritanceLaw.All) - Government.InheritanceRate(InheritanceLaw.High);
                return (mass ? 1.0 : 0.0) - 3.0 * step * e.Wealth[b];
            }
            case PolicyAxis.MonetaryStandard:
            {
                bool silver = System.Math.Max(0, (int)w.MonetaryLaw - shift) == (int)MonetaryStandardLaw.Silver;
                var c = w.Coef;
                double drop = 0, peer = 0;
                if (silver) { drop = PoliticalQuantities.Clamp01(1.0 - w.SilverGoldRatio); if (!(drop >= 0 && drop <= 1)) drop = 0; }
                if (c.MonetaryPeerPullScale > 0 && silver)
                {
                    peer = c.MonetaryPeerPullScale * System.Math.Abs(1.0 - w.SilverGoldRatio) * w.TradeGoldPull;
                    if (!(peer >= -c.MonetaryPeerPullScale && peer <= c.MonetaryPeerPullScale)) peer = 0;
                }
                double infl = w.ExpectedInflationPerYear.Value;
                return c.MonetaryPrefScale * (e.Wealth[b] * (System.Math.Max(0, infl) + drop + peer) - (1.0 - e.Wealth[b]) * (System.Math.Max(0, -infl) + drop));
            }
            default: return double.NaN;
        }
    }

    /// <summary>純支持の全ての項（街頭の 3 陣営・議会・国庫・軍）を計器が組む。inc＝false の newRule は段に依る軸を 1 段下で読む。</summary>
    static double LdNet(World w, PolicyAxis a, bool inc, in PolicyPreferences.Profile e, bool newRule)
    {
        double sign = inc ? 1 : -1;
        bool levelAxis = a is PolicyAxis.Suffrage or PolicyAxis.Inheritance or PolicyAxis.MonetaryStandard;
        int shift = !inc && newRule ? 1 : 0;
        var ee = e;
        double P(int b) => levelAxis ? LdLevelPref(w, a, b, ee, shift) : PolicyPreferences.Pref(a, b, ee, w, inc);
        var c = w.Coef;
        bool leg = w.ParliamentEnabled && PolicyPreferences.HasLegislature(w.Polity);
        double street = c.ParliamentStreetSupersede > 0 && leg ? 1.0 - PoliticalQuantities.Clamp01(c.ParliamentStreetSupersede) : 1.0;
        double sum = 0;
        for (int b = 0; b < PolicyPreferences.BlocCount; b++) sum += street * e.Power[b] * P(b) * sign;
        if (leg)
        {
            double parl = 0;
            for (int party = 0; party < PolicyPreferences.BlocCount; party++) parl += w.PartySeats[party] * P(party) * sign;
            sum += c.ParliamentLawWeight * parl;
        }
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax) sum += e.StateRevenue * sign;
        sum += PolicyPreferences.ArmyPower(w) * PolicyPreferences.ArmyStake(a) * sign;
        return sum;
    }

    /// <summary>四半期の末：年末なら計器の国ごと・軸ごとに 1 行。</summary>
    static void LawDirTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("lawdir_s")) return;
        if (q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1) return;
        var sk = LdSinks.GetValue(sim, _ => new LdSink());
        if (sk.W is null) { sk.W = Open("lawdir", seed); sk.W.WriteLine(LawDirHeaderText); }
        sk.Peek.Clear();
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!IsPicked(w, S(w)) || !(w.TotalPopulation > 0)) continue;
            var e = PolicyPreferences.Build(w);
            foreach (var a in FactionSnapshot.Axes)
            {
                if (a == PolicyAxis.NationalizationAll) continue;
                string line = string.Join('\t', q.ToString(inv), w.Year.ToString(inv), NameOf(w), a.ToString(), F(Government.LevelPosition(w, a)),
                    F(PolicyPreferences.Pref(a, 0, e, w)), F(PolicyPreferences.Pref(a, 1, e, w)), F(PolicyPreferences.Pref(a, 2, e, w)),
                    F(LdNet(w, a, true, e, false)), F(LdNet(w, a, false, e, false)), F(LdNet(w, a, true, e, true)), F(LdNet(w, a, false, e, true)),
                    F(PolicyPreferences.NetSupport(w, a, true, e)), F(PolicyPreferences.NetSupport(w, a, false, e)),
                    (w.Coef.LawPrefSinglePeaked > 0 ? 1 : 0).ToString(inv));
                sk.W.WriteLine(line); sk.Peek.Add(line);
            }
        }
    }

    static void LawDirClose(MultiNationSim sim) { if (LdSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
