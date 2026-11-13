namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (821) 列強の調停の戦争の講和の表 cpmed_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpeace.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（行の一覧は計器の中だけ）。
// 目的＝旗 ForcedPeaceBlankOverCollapse（調停の戦争は崩壊の上書きを通らず白紙で終わる）の旧新を同じ盤面で読む。旗 0 でも旗 1 でも同じ列を書く。
// 取り方＝講和 1 本ごと（内戦を含む）に Diplomacy.ConcludeWar が ResolvePeace の前（CmBefore）と直後（CmAfter）で呼ぶ。
//   ⚠期待の側（blankFlag・overwriteMatchFlag・repWouldOldIndex・regionsMovedCount）はエンジンの関数を呼ばず、講和の前の入力から計器が式で書く。
//   実際の側＝repMovedIndex（ResolvePeace が移した rep）・ledgerAbsIndex（両国の国庫の台帳 WarCost の前後の差の絶対値の和＝台帳の側）・capturedCount・cededFlag。
// 列：seq／month／year／sideA／sideB／civilFlag／forcedFlag／collapsedIterFlag（その戦争の月の歩で崩壊と判定されたか＝CpIter の控え）／exA／exB（講和の前の厭戦）／scoreIndex（講和の前の未クランプの戦勝点）／
//   blankFlag（|Clamp(戦勝点,−1,1)| ＜ WhitePeaceScore）／overwriteMatchFlag（|戦勝点| が崩壊の上書きの式 1.5×min(1,max(0,2×勝者の押し÷(勝者の押し＋敗者の押し＋1e-12))) と A・B どちらかを敗者と見て一致）／
//   hitFlag（ResolvePeace が賠償の枝まで進んだ）／repMovedIndex／ledgerAbsIndex／capturedCount／cededFlag／regionsMovedCount（A・B の国の州の数の前後の差の絶対値の和）／
//   repWouldOldIndex（調停の戦争で collapsedIterFlag=1 の講和だけ＝旧の規則の反実仮想 ReparationGdpFraction×敗者の GdpPerMonth×sa×敗者の FiscalFundingRatio・sa＝min(1,1.5×reach)・sa が WhitePeaceScore 未満なら 0〔旧でも白紙〕・それ以外の行は 0）。
//   敗者＝崩壊の線（厭戦≧1−RevolutionSurrenderWeight×革命圧力）にいる側・両者なら厭戦が大きい側（同値は押しの累計が小さい側）。
// ⚠限界（計器が自分で申告する）：①collapsedIterFlag はエンジンの判定の控え②repWouldOld の同値の敗者決めは押しの累計（歩の本体は塹壕の後のその月の押し）
//   ③repWouldOld は会議の削り（repCut）と条約港の倍率の前・(791) の 1 回分の取り分の前（旧の規則で同じ敗者の崩壊の戦争と集合になれば実際は取り分だけ）
//   ④ledgerAbsIndex は ResolvePeace の中で動いた WarCost の全て（賠償以外の WarCost の動きがあれば混ざる）⑤国を持たない側（抽象標的）の台帳と州は 0 と数える。
public static partial class Instruments
{
    internal const string CpmedHeaderText = "seq\tmonth\tyear\tsideA\tsideB\tcivilFlag\tforcedFlag\tcollapsedIterFlag\texA\texB\tscoreIndex\tblankFlag\toverwriteMatchFlag\thitFlag\trepMovedIndex\tledgerAbsIndex\tcapturedCount\tcededFlag\tregionsMovedCount\trepWouldOldIndex";

    internal sealed class CmPre
    {
        public string Row0 = "";
        public double LedA, LedB; public int RegA, RegB;
        public double RepWouldOld;
    }
    sealed class CmState { public int Seq; public readonly System.Collections.Generic.List<string> Rows = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, CmState> CmStates = new();
    static CmState CmOf(Diplomacy d) => CmStates.GetValue(d, _ => new CmState());

    static double CmLed(Power p) => p.Nation is { } w ? w.TreasuryLedger[(int)TreasuryCat.WarCost] : 0.0;
    static int CmReg(Power p) => p.Nation is { } w ? w.Regions.Count : 0;
    static double CmImpose(double winPush, double losePush)
    {
        double q = 2.0 * winPush / (winPush + losePush + 1e-12);
        if (!(q > 0)) q = 0; if (q > 1) q = 1;
        return 1.5 * q;
    }
    static bool CmSame(double a, double b) => System.Math.Abs(a - b) <= 1e-12 * System.Math.Max(1.0, System.Math.Abs(b));

    /// <summary>講和の前（ResolvePeace を呼ぶ前）：入力から式で書ける物を記録する。</summary>
    internal static CmPre CmBefore(Diplomacy d, Coefficients c, War war)
    {
        var st = CmOf(d); var cps = CpOf(d);
        bool civil = war.CivilWarParent is not null, forced = war.ForcedPeace;
        bool collapsed = CpIterFlags.TryGetValue(war, out var cf) && cf.Value;
        double exA = war.A.WarExhaustion, exB = war.B.WarExhaustion, sc = war.Score;
        double s = double.IsNaN(sc) ? 0 : System.Math.Max(-1.0, System.Math.Min(1.0, sc));
        bool blank = System.Math.Abs(s) < c.WhitePeaceScore;
        double abs = System.Math.Abs(sc);
        bool match = CmSame(abs, CmImpose(war.PushB, war.PushA)) || CmSame(abs, CmImpose(war.PushA, war.PushB));   // A が敗者（勝者の押し＝PushB）か B が敗者か
        double would = 0;
        if (forced && collapsed && !civil)
        {
            // 崩壊の線は厭戦≧1−RevolutionSurrenderWeight×革命圧力（入力の読み＝Politics.RevolutionPressure は状態を書かない）。両者が線にいれば厭戦が大きい側（同値は押しの累計が小さい側）が敗者。
            double CutOf(Power q) => 1.0 - (q.Nation is { } qn ? c.RevolutionSurrenderWeight * Politics.RevolutionPressure(qn) : 0.0);
            bool ac = exA >= CutOf(war.A), bc = exB >= CutOf(war.B);
            bool aLoses = ac && bc ? (exA != exB ? exA > exB : war.PushA < war.PushB) : ac || !bc && exA > exB;
            Power loser = aLoses ? war.A : war.B, winner = aLoses ? war.B : war.A;
            double winPush = aLoses ? war.PushB : war.PushA, losePush = aLoses ? war.PushA : war.PushB;
            double sa = System.Math.Min(1.0, CmImpose(winPush, losePush));
            if (sa >= c.WhitePeaceScore && loser.Nation is { } lw && winner.Nation is not null)   // 旧の規則でも |戦勝点| が白紙の線の内なら払わない
                would = c.ReparationGdpFraction.Value * loser.GdpPerMonth.Value * sa * lw.FiscalFundingRatio.Value;
        }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string row0 = string.Join('\t', I(++st.Seq), I(cps.Month), I(cps.Year), NameOf(war.A), NameOf(war.B), I(civil ? 1 : 0), I(forced ? 1 : 0), I(collapsed ? 1 : 0), F(exA), F(exB), F(sc), I(blank ? 1 : 0), I(match ? 1 : 0));
        return new CmPre { Row0 = row0, LedA = CmLed(war.A), LedB = CmLed(war.B), RegA = CmReg(war.A), RegB = CmReg(war.B), RepWouldOld = would };
    }

    /// <summary>ResolvePeace の直後：実際に動いた値を足して行を積む。</summary>
    internal static void CmAfter(Diplomacy d, War war, CmPre pre)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool hit = d.CpHit;
        double led = System.Math.Abs(CmLed(war.A) - pre.LedA) + System.Math.Abs(CmLed(war.B) - pre.LedB);
        int reg = System.Math.Abs(CmReg(war.A) - pre.RegA) + System.Math.Abs(CmReg(war.B) - pre.RegB);
        CmOf(d).Rows.Add(string.Join('\t', pre.Row0, I(hit ? 1 : 0), F(hit ? d.CpRep : 0.0), F(led), I(hit ? d.CpCaptured : 0), I(hit && d.CpCeded ? 1 : 0), I(reg), F(pre.RepWouldOld)));
    }

    static void CmTable(Sink sk, Diplomacy d)
    {
        if (!CmStates.TryGetValue(d, out var st)) return;
        using var wr = Open("cpmed", sk.Seed);
        wr.WriteLine(CpmedHeaderText);
        foreach (var line in st.Rows) wr.WriteLine(line);
    }

    /// <summary>行の文字列の一覧（表に書く行そのもの・試験が読む）。</summary>
    internal static System.Collections.Generic.List<string> CmRowsPending(Diplomacy d)
        => CmStates.TryGetValue(d, out var st) ? new(st.Rows) : new();
}
