namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (389) AI の官僚制度の判断の表 burblock_s（読むだけ・事前登録 measurements/ab_20261017_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAnchor.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// SelfPlay.GateAllLawTargets の先頭（畳まれる前）で AI が〈官僚制度が前提の法〉を欲しがった四半期（または官僚制度の目標が現行と違う四半期）に 1 行。
//   bur ＝ 現行の官僚制度（0 猟官・1 資格任用・2 専門官僚）／burIn ＝ 走査の前の目標／burOut ＝ 走査の後の目標（旗 0 では burIn と同じ）。
//   wantMask ＝ その一歩が官僚制度に左右される法を欲しい（1 社会保障〔失業保険以上〕・2 独占禁止・4 国有化〔5 部門のどれか〕・8 国有化〔全て〕・16 累進課税・32 その他）／blockMask ＝さらに今の官僚制度では足りない。
//   feasMask ＝ blockMask のうち、官僚制度が足りていれば法律タブの門（GateInfo＝純支持・政治資本の残り）も通る法（旗 2 の対象）。
//   need ＝ 足りないときに必要な段（blockMask が 0 なら −1）。
// ⚠限界：1 四半期に複数回走れば最後の 1 行だけ。AI でない国（プレイヤー）は通らない。勢力の要求・議会の綱領が直接置いた目標の前提落ちは数えない。
public static partial class Instruments
{
    internal const string BurHeaderText = "quarter\tyear\tnation\tbur\tburIn\tburOut\twantMask\tblockMask\tfeasMask\tneed";

    /// <summary>GateAllLawTargets の先頭から呼ぶ（Instruments.On のときだけ）。行は Quarter の BurFold が書く。</summary>
    internal static void BurObserve(World w, BureaucracyLaw burIn, BureaucracyLaw burOut, int wantMask, int blockMask, int feasMask, int need)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        S(w).BurRow = string.Join('\t', I((int)w.Bureaucracy), I((int)burIn), I((int)burOut), I(wantMask), I(blockMask), I(feasMask), I(need));
    }

    static void BurFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        if (st.BurRow is not { } row) return;
        st.BurRow = null;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Bur ??= Open("burblock", seed);
        if (!sk.BurHeader) { sk.Bur.WriteLine(BurHeaderText); sk.BurHeader = true; }
        sk.Bur.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), row));
    }
}
