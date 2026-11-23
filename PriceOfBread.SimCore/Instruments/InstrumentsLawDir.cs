namespace PriceOfBread.SimCore;

// (1323) c138lp 計器 lawdir_s：法の好みの向き（拡大と後退）の純支持を、旧の規則（後退＝拡大の符号の反転）と新の規則（望む段からの単峰の 1 段の損得）で
// 同じ盤面に並べ、エンジンが実際に使う純支持（公開の NetSupport）と同じ行に書く。計器の国の年末だけ・軸ごとに 1 行。
// 旧と新の式は事前登録 measurements/m1323_20261123_prereg_lp.txt の文から計器が自分で組む（エンジンの Directed・SinglePeaked を呼ばない）。
// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（Build は窓の外で呼ぶ＝POP の窓に値を残さない）。
public static partial class Instruments
{
    internal const string LawDirHeaderText = "quarter\tyear\tnation\taxis\tlevelShare\tprefTradIndex\tprefModernIndex\tprefMassIndex"
        + "\tnetUpOldIndex\tnetDownOldIndex\tnetUpNewIndex\tnetDownNewIndex\tnetUpUsedIndex\tnetDownUsedIndex\tsinglePeakedFlag";

    sealed class LdSink { public System.IO.StreamWriter? W; public readonly List<string> Peek = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, LdSink> LdSinks = new();

    /// <summary>試験の読み口：直前に書いた年末の行。</summary>
    internal static IReadOnlyList<string> LawDirPeek(MultiNationSim sim) => LdSinks.GetValue(sim, _ => new LdSink()).Peek;

    /// <summary>事前登録の新の規則（計器の写し）：強さ |p|・望む段の差 c＝(1+sgn(p)+p)/2・段 k→k+d の好み＝|p|×(|c|−|c−d|)。</summary>
    static double LdNew(double p, int d)
    {
        double sg = p > 0 ? 1.0 : p < 0 ? -1.0 : 0.0;
        double c = (1.0 + sg + p) / 2.0;
        return System.Math.Abs(p) * (System.Math.Abs(c) - System.Math.Abs(c - d));
    }

    /// <summary>純支持の全ての項（街頭の 3 陣営・議会・国庫・軍）を計器が組む。newRule＝false は p×sign。</summary>
    static double LdNet(World w, PolicyAxis a, bool inc, in PolicyPreferences.Profile e, bool newRule)
    {
        int d = inc ? 1 : -1;
        double F(double p) => newRule ? LdNew(p, d) : p * d;
        var c = w.Coef;
        bool leg = w.ParliamentEnabled && PolicyPreferences.HasLegislature(w.Polity);
        double street = c.ParliamentStreetSupersede > 0 && leg ? 1.0 - PoliticalQuantities.Clamp01(c.ParliamentStreetSupersede) : 1.0;
        double sum = 0;
        for (int b = 0; b < PolicyPreferences.BlocCount; b++) sum += street * e.Power[b] * F(PolicyPreferences.Pref(a, b, e, w, inc));
        if (leg)
        {
            double parl = 0;
            for (int party = 0; party < PolicyPreferences.BlocCount; party++) parl += w.PartySeats[party] * F(PolicyPreferences.Pref(a, party, e, w, inc));
            sum += c.ParliamentLawWeight * parl;
        }
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax) sum += F(e.StateRevenue);
        sum += PolicyPreferences.ArmyPower(w) * F(PolicyPreferences.ArmyStake(a));
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
