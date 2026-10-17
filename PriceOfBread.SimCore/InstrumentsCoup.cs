namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (393) 軍の力の入口の表 coupgate_s（読むだけ・事前登録 measurements/mc_20261018_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsBur.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// Politics.Transition の第 1 の門（引力点が現政体と違い、支持差が慣性を超えた）を通った国×四半期に 1 行。旗 HoldSplitBySoldierIdeology が 0 でも出す
//   ＝旗 0 の表がそのまま〈今、兵の分けが入ったら門 2 はどう動くか〉の表になる。
//   polity／attractor ＝ (int)Polity（現政体／引力点）。sCur／sAtt ＝ 兵士 POP だけの集計での現政体／引力点の支持（負もそのまま）。a ＝ 兵士の支持の比（Politics.SoldierHoldShare）。
//   soldierW ＝ 兵士の集計の分母（規模×意識×発言権の和）。0 なら兵士の思想が読めない縁で a＝1。
//   kappa ＝ 分ける前の実効保持力。challenge ＝ その時の動員（旗 0 は兵を含み、旗 1 は兵を含まない）。
//   passOld ＝ challenge ＞ kappa（旧の門 2）。passSplit ＝ challenge ＋ kappa×(1−a) ＞ kappa×a（分けた門 2）。1／0。
// ⚠限界：1 四半期に Transition が複数回走れば最後の行だけ。passOld は旗 1 では動員から兵を外した値で比べる（旗 0 の passOld と同じ量ではない）。上からの改革の例外（reformFromAbove）は門 2 の外なので passOld／passSplit に入れていない。
public static partial class Instruments
{
    internal const string CoupHeaderText = "quarter\tyear\tnation\tpolity\tattractor\tsCur\tsAtt\ta\tsoldierW\tkappa\tchallenge\tpassOld\tpassSplit";

    /// <summary>Transition の第 1 の門の直後から呼ぶ（Instruments.On のときだけ）。行は Quarter の CoupFold が書く。</summary>
    internal static void CoupGate(World w, Polity attractor, double sCur, double sAtt, double a, double soldierW, double kappa, double challenge)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string D(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        int passOld = challenge > kappa ? 1 : 0;
        int passSplit = challenge + kappa * (1.0 - a) > kappa * a ? 1 : 0;
        S(w).CoupRow = string.Join('\t', I((int)w.Polity), I((int)attractor), D(sCur), D(sAtt), D(a), D(soldierW), D(kappa), D(challenge), I(passOld), I(passSplit));
    }

    static void CoupFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        if (st.CoupRow is not { } row) return;
        st.CoupRow = null;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Coup ??= Open("coupgate", seed);
        if (!sk.CoupHeader) { sk.Coup.WriteLine(CoupHeaderText); sk.CoupHeader = true; }
        sk.Coup.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), row));
    }
}
