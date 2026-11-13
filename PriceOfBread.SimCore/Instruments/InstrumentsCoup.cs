namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsCoup.cs#1〕
public static partial class Instruments
{
    internal const string CoupHeaderText = "quarter\tyear\tnation\tpolity\tattractor\tsCur\tsAtt\ta\tsoldierW\tkappa\tchallenge\tpassOld\tpassSplit\tatWar\tsoldierMob\troute\tnanBranchMark";

    /// <summary>Transition の第 1 の門の直後から呼ぶ（Instruments.On のときだけ）。行は Quarter の CoupFold が書く。</summary>
    internal static void CoupGate(World w, Polity attractor, double sCur, double sAtt, double a, double soldierW, double kappa, double challenge)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string D(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        int passOld = challenge > kappa ? 1 : 0;
        int passSplit = challenge + kappa * (1.0 - a) > kappa * a ? 1 : 0;
        var st = S(w);
        st.CoupRoute = 0; st.CoupNanBranch = 0;
        st.CoupRow = string.Join('\t', I((int)w.Polity), I((int)attractor), D(sCur), D(sAtt), D(a), D(soldierW), D(kappa), D(challenge), I(passOld), I(passSplit),
            Government.AtWar(w) ? "1" : "0", D(SoldierMobShare(w, w.Coef)));
    }

    /// <summary>Transition の第 1 の門の後の出口の印（Instruments.On のときだけ呼ばれる）。</summary>
    internal static void CoupRoute(World w, int route, bool violent = true) { var st = S(w); st.CoupRoute = route; st.CoupNanBranch = route == 5 && !violent ? 1 : 0; }   // CoupRoute〔履歴 InstrumentsCoup.cs#2〕

    /// <summary>動員の素の量（Politics.MeasureMobilization の改革派＋急進派の和・外圧の追い風と失望の割引の前・組合の動員は除く）に兵士 POP が占める割合。
    /// ⚠式の写し（読むだけ）：MeasureMobilization の POP ごとの 2 項（規模×意識〔改革＋急進の和〕・窮乏のチャネル）を同じ係数で組み直す。式が変わったらここも直す。分母が 0 なら 0。</summary>
    static double SoldierMobShare(World w, Coefficients c)
    {
        double sold = 0, all = 0;
        foreach (var p in w.AllPops())
        {
            double m = p.Size * p.Consciousness + p.Size * System.Math.Max(0, p.Militancy - c.RiotMilitancyThreshold) * c.DesperationChallengeWeight * (1.0 - c.DesperationConsciousnessGate * p.Consciousness);
            all += m;
            if (p.IsSoldier) sold += m;
        }
        return all > 0 ? sold / all : 0.0;
    }

    static void CoupFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        if (st.CoupRow is not { } row) return;
        st.CoupRow = null;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Coup ??= Open("coupgate", seed);
        if (!sk.CoupHeader) { sk.Coup.WriteLine(CoupHeaderText); sk.CoupHeader = true; }
        sk.Coup.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), row, I(st.CoupRoute), I(st.CoupNanBranch)));
    }
}
