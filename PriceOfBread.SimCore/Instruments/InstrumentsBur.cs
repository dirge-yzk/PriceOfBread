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

    /// <summary>走査の実時間を測る（SelfPlay.GateAllLawTargets が Instruments.On のときだけ呼ぶ）。.cost の件数に走査は入らないので別の物差し。</summary>
    internal static void BurTime(World w, long ticks) { var st = S(w); st.BurTicks += ticks; st.BurCalls++; }

    /// <summary>試合の終わりに 1 行：走査の呼びの数・実時間（秒）・国の数。死んだ国の分は含まない（下限）。上限の 248.5 秒に対する余りと比べる。
    /// ⚠旗 0 の計器つきでも走査は走る（読むだけの記録）＝この秒は旗 2 の走査の費用の上限に近い（立場の土台は直後の GateLaw と分け合うので、旗 0 なら後で作る分の前倒しを含む）。</summary>
    static void BurScanTable(Sink sk, MultiNationSim sim)
    {
        long ticks = 0; int calls = 0, nations = 0;
        foreach (var n in sim.Nations) { var st = S(n.World); ticks += st.BurTicks; calls += st.BurCalls; nations++; }
        if (calls == 0) return;
        using var f = Open("burscan", sk.Seed);
        f.WriteLine("calls\tseconds\tnations");
        f.WriteLine(string.Join('\t', calls.ToString(System.Globalization.CultureInfo.InvariantCulture), (ticks / (double)System.Diagnostics.Stopwatch.Frequency).ToString("R", System.Globalization.CultureInfo.InvariantCulture), nations.ToString(System.Globalization.CultureInfo.InvariantCulture)));
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
