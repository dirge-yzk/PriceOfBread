namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (393) 軍の力の入口の表 coupgate_s（読むだけ・事前登録 measurements/mc_20261018_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsBur.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// Politics.Transition の第 1 の門（引力点が現政体と違い、支持差が慣性を超えた）を通った国×四半期に 1 行。
//   （(393) の採用で旗 HoldSplitBySoldierIdeology は焼き込んだ。旗 0 の腕 A の表は〈兵の分けが入ったら門 2 はどう動くか〉の表だった。measurements/b10_20261018_mc393_A_ins と同 an_mc。）
//   polity／attractor ＝ (int)Polity（現政体／引力点）。sCur／sAtt ＝ 兵士 POP だけの集計での現政体／引力点の支持（負もそのまま）。a ＝ 兵士の支持の比（Politics.SoldierHoldShare）。
//   soldierW ＝ 兵士の集計の分母（規模×意識×発言権の和）。0 なら兵士の思想が読めない縁で a＝1。
//   kappa ＝ 分ける前の実効保持力。challenge ＝ その時の動員（兵を含まない）。
//   passOld ＝ challenge ＞ kappa（分けない門 2）。passSplit ＝ challenge ＋ kappa×(1−a) ＞ kappa×a（分けた門 2）。1／0。
//   a ＝ 1−d（d＝兵士の離反の割合・Ideologies.DefectionShare）。sCur／sAtt は集計での支持（a の計算には使わない・読むだけ）。
//   atWar ＝ Government.AtWar（1／0）。soldierMob ＝ 動員の素の量（外圧の追い風と失望の割引の前）に兵士 POP が占める割合（動員から外れた量の大きさ・組合の動員は除く）。
//   route ＝ 第 1 の門の後の経路：1＝門 2 で止まる／2＝平和な漸進で止まる／3＝首都の蜂起／4＝下賜（議会統制力の床だけ書いて戻る）／5＝暴力の即時の政変／6＝非暴力の即時の政変／7＝内戦中・蜂起の予約中で止まる／0＝出口の印が無い（あってはならない）。
// ⚠限界：1 四半期に Transition が複数回走れば最後の行だけ。passOld は動員から兵を外した値で比べる（採用前の旗 0 の passOld は兵を含む動員で比べていた＝同じ量ではない）。上からの改革の例外（reformFromAbove）は門 2 の外なので passOld／passSplit に入れていない。
public static partial class Instruments
{
    internal const string CoupHeaderText = "quarter\tyear\tnation\tpolity\tattractor\tsCur\tsAtt\ta\tsoldierW\tkappa\tchallenge\tpassOld\tpassSplit\tatWar\tsoldierMob\troute";

    /// <summary>Transition の第 1 の門の直後から呼ぶ（Instruments.On のときだけ）。行は Quarter の CoupFold が書く。</summary>
    internal static void CoupGate(World w, Polity attractor, double sCur, double sAtt, double a, double soldierW, double kappa, double challenge)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string D(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        int passOld = challenge > kappa ? 1 : 0;
        int passSplit = challenge + kappa * (1.0 - a) > kappa * a ? 1 : 0;
        var st = S(w);
        st.CoupRoute = 0;
        st.CoupRow = string.Join('\t', I((int)w.Polity), I((int)attractor), D(sCur), D(sAtt), D(a), D(soldierW), D(kappa), D(challenge), I(passOld), I(passSplit),
            Government.AtWar(w) ? "1" : "0", D(SoldierMobShare(w, w.Coef)));
    }

    /// <summary>Transition の第 1 の門の後の出口の印（Instruments.On のときだけ呼ばれる）。</summary>
    internal static void CoupRoute(World w, int route) => S(w).CoupRoute = route;

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
        sk.Coup.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), row, I(st.CoupRoute)));
    }
}
