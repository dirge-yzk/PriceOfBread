namespace PriceOfBread.SimCore;

// 目的＝段 2 の器（旗 WarExitFromCapacity）の旧と新の規則を、旗 0 の盤面で戦争×月に並べて数える（恒等の計器）。
// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（行の一覧は計器の中だけ）。
// 計器は自分で式を書く（エンジンの WarCapacity・WarEnds・RevolutionPressure・InCivilWar を呼ばない）。
public static partial class Instruments
{
    internal const string WexitHeaderText = "seq\tmonth\tyear\tsideA\tsideB\tcivilFlag\tforcedFlag\tscoreIndex\tpushSumPositiveFlag\tkA\tkB\toldCollapseAFlag\toldCollapseBFlag\toldDecisiveFlag\toldWearyFlag\tnewEndsFlag\tcivilWarAFlag\tcivilWarBFlag\toldCrisisAFlag\toldCrisisBFlag\tpowerIdA\tpowerIdB";

    sealed class WxState { public int Seq; public readonly System.Collections.Generic.List<string> Rows = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, WxState> WxStates = new();
    /// <summary>戦争ごとの前の月の累計の押し（A・B）。月の押し＝今の累計−前の累計（同点の決め手に使う・計器の側の辞書でエンジンの欄は足さない）。</summary>
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<War, double[]> WxPrevPush = new();

    /// <summary>計器が自分で数える：その国が内戦の当事かどうか（戦争の一覧を回す）。国のない側は偽。</summary>
    static bool WxInCivil(Diplomacy d, Power p)
    {
        if (p.Nation is null) return false;
        foreach (var w in d.Wars) if (w.CivilWarParent is not null && (w.A == p || w.B == p)) return true;
        return false;
    }
    /// <summary>強制力 ÷(強制力＋挑戦)（計器の自前の式・国のない側と分母 0 は 1）。</summary>
    static double WxK(Power p, Coefficients c)
    {
        if (p.Nation is not { } n) return 1.0;
        double cc = n.Coercion * (n.TotalPopulation / System.Math.Max(1.0, c.PoliticalReferencePop)), xx = n.ReformistPower + n.RadicalPower;
        double den = cc + xx;
        return !(den > 0) ? 1.0 : cc / den;
    }
    /// <summary>旧の崩壊の線の下げ幅 1−RevolutionSurrenderWeight×革命圧力（計器の自前の式）。</summary>
    static double WxCut(Power p, Coefficients c)
    {
        if (p.Nation is not { } n) return 1.0;
        double coerce = n.Coercion * (n.TotalPopulation / System.Math.Max(1.0, c.PoliticalReferencePop));
        double rp = System.Math.Max(0.0, System.Math.Min(1.0, (n.ReformistPower + n.RadicalPower) / System.Math.Max(1.0, coerce)));
        return 1.0 - c.RevolutionSurrenderWeight * rp;
    }
    /// <summary>旧の国難 5 項（厭戦・内戦・食・支払い率・正統性）を計器が自分で読む。</summary>
    static bool WxOldCrisis(Diplomacy d, Power p, Coefficients c)
        => p.WarExhaustion > c.WarWearyThreshold
           || (p.Nation is { } w && (WxInCivil(d, p) || !(w.FoodFill >= c.AllyJoinFoodMin) || !(w.FiscalFundingRatio >= c.AllyJoinFundingMin) || !(w.Legitimacy >= c.AllyJoinLegitimacyMin)));

    /// <summary>月の戦況の更新の直後（出口の判定の前）に 1 戦争 1 行。</summary>
    internal static void WexitRow(Diplomacy d, Coefficients c, War war, double scoreBeforeCollapse)
    {
        if (!On || !Tg.Cede || !Wants("wexit_s")) return;
        var st = WxStates.GetValue(d, _ => new WxState()); var cps = CpOf(d);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var prevPush = WxPrevPush.GetValue(war, _ => new double[2]);
        double monthA = war.PushA - prevPush[0], monthB = war.PushB - prevPush[1];   // 旧の同点の決め手＝その月の押し（計器は累計の差で数える）
        prevPush[0] = war.PushA; prevPush[1] = war.PushB;
        double kA = WxK(war.A, c), kB = WxK(war.B, c), s = scoreBeforeCollapse;   // 旧の崩壊が war.Score を書き換える前の値（oldDecisiveFlag だけは書き換えた後の値 sOld で数える）
        double cutA = WxCut(war.A, c), cutB = WxCut(war.B, c);
        bool colA = war.A.WarExhaustion >= 1.0 * cutA, colB = war.B.WarExhaustion >= 1.0 * cutB;
        bool weary = !(war.A.WarExhaustion <= c.WarPeaceExhaustion * cutA) && !(war.B.WarExhaustion <= c.WarPeaceExhaustion * cutB);
        bool ends = s >= kB || -s >= kA;
        // 旧の崩壊が書き換えた後の戦勝点（計器の自前の式）：崩壊の側がある（かつ列強の調停の空白でない）なら ±1.5×取り分、無ければ s。
        double sOld = s;
        if ((colA || colB) && !(c.ForcedPeaceBlankOverCollapse > 0 && war.ForcedPeace))
        {
            bool aLoses = colA && colB ? (war.A.WarExhaustion != war.B.WarExhaustion ? war.A.WarExhaustion > war.B.WarExhaustion : monthA < monthB) : colA;   // 同点の決め手は旧の規則と同じく月の押し（前の月の累計との差）
            double winPush = aLoses ? war.PushB : war.PushA, losePush = aLoses ? war.PushA : war.PushB;
            double reach = System.Math.Max(0.0, System.Math.Min(1.0, 2.0 * winPush / (winPush + losePush + 1e-12)));
            double impose = Diplomacy.CollapseScore * reach;
            sOld = aLoses ? -impose : impose;
        }
        st.Rows.Add(string.Join('\t', I(++st.Seq), I(cps.Month), I(cps.Year), NameOf(war.A), NameOf(war.B), I(war.CivilWarParent is not null ? 1 : 0), I(war.ForcedPeace ? 1 : 0), F(s),
            I(war.PushA + war.PushB > 0 ? 1 : 0), F(kA), F(kB), I(colA ? 1 : 0), I(colB ? 1 : 0), I(System.Math.Abs(sOld) >= 1.0 ? 1 : 0), I(weary ? 1 : 0), I(ends ? 1 : 0),
            I(WxInCivil(d, war.A) ? 1 : 0), I(WxInCivil(d, war.B) ? 1 : 0), I(WxOldCrisis(d, war.A, c) ? 1 : 0), I(WxOldCrisis(d, war.B, c) ? 1 : 0), I(war.A.Id), I(war.B.Id)));   // 末尾の 2 列＝戦争の両側の Power.Id（読むだけ）
    }

    static void WexitTable(Sink sk, Diplomacy d)
    {
        if (!Tg.Cede || !Wants("wexit_s")) return;
        if (!WxStates.TryGetValue(d, out var st)) return;
        using var wr = Open("wexit", sk.Seed);
        wr.WriteLine(WexitHeaderText);
        foreach (var line in st.Rows) wr.WriteLine(line);
    }
}
