namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (384) 政治の束の計器（読むだけ・事前登録 measurements/pb_20261017_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsScalars.cs／InstrumentsFm.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・World に状態を足さない
// （積む先は World ごとの ConditionalWeakTable＝セーブに載らない）。呼ぶ側は `if (Instruments.On)` で守る＝既定は 1 命令も走らない。scalars_s の末尾の列が読む。
//   pbDomActive ＝ 一党支配の引力（DominanceCoupWeight×議席の過剰）が非ゼロだった Politics.Transition の呼びの数（その World の累計）。
//   pbDomFlip   ＝ そのうち、引力あり・なしで引力点が違った呼びの数。
//   pbDomDecisive ＝ そのうち、引力ありの引力点が一党支配の独裁形態で、現政体と違い、第 1 の門（支持差が保持力 PolityInertia×正統性 を超える）を越えた呼びの数。以降の門（動員が保持力を超える等）は含まない＝上限。
//   pbDomExec   ＝ そのうち、同じ呼びの中で ExecuteRegimeChange が引力の独裁形態を目標に実行された数（Transition の後ろの門を全て越えた＝引力が実際に政体を変えた上限に近い数。同じ四半期の別の経路が同じ目標へ実行しても数える）。
//   pbDomMismatch ＝ 計器の複製（Instruments.PbPick）が本体のループと違う引力点・支持を出した呼びの数（0 でなければ複製がずれている＝計器の信頼を落とす）。
//   pbAgil ＝ Government.Move で非議会政体の歩幅に勅令の速さが掛かった歩の数。pbSeize ＝ World.Seize で資本家 POP の闘争度が動いた接収の数（SeizureMilitancyCost の読み手が走った数）。
//   pbRegime／pbRegimeViolent／pbRegimeDict ＝ Politics.ExecuteRegimeChange の呼びの数／そのうち暴力の転換（violent）の数／そのうち目標が独裁（BD・PD・MD）の数。
// ⚠限界（計器が自分で申告する）：累計は World ごと＝分離独立で生まれた国は 0 から数え直す（変名しても World は同じ）。pbDomExec の〈決定的〉の記録（PullTarget）は次に Transition が走るまで残る＝Transition を踏まない四半期に別の経路（SettleRevolt など）が同じ独裁形態へ実行しても数える（上限の数）。引力の反事実は〈引力だけを 0 にした〉1 呼びの反事実で、引力が無い世界の軌道ではない。
public static partial class Instruments
{
    internal sealed class PbAcc { public double DomActive, DomFlip, DomDecisive, DomExec, DomMismatch, Regime, RegimeViolent, RegimeDict, Agil, Seize; public Polity? PullTarget; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, PbAcc> PbAccs = new();
    internal static PbAcc PbOf(World w) => PbAccs.GetValue(w, _ => new PbAcc());

    /// <summary>★(384) Politics.Transition の呼び（一党支配の引力が非ゼロのとき）：引力あり・なしの引力点の違い。</summary>
    internal static void PbDominance(World w, bool flip, bool decisive, bool mismatch, Polity target)
    {
        var a = PbOf(w); a.DomActive += 1;
        if (flip) a.DomFlip += 1;
        if (decisive) { a.DomDecisive += 1; a.PullTarget = target; }
        if (mismatch) a.DomMismatch += 1;
    }

    /// <summary>★(384) Politics.Transition の引力点の求め方の複製（計器専用・読むだけ）。<paramref name="pullWeight"/> だけを差し替えて、一党支配の引力が引力点を変えたかを反事実で数える。
    /// 本体のループ（Politics.Transition）と同じ式・同じ演算の順＝<c>pbDomMismatch</c> が本体との食い違いを数える。Instruments.On のときだけ呼ばれる。</summary>
    static (Polity attractor, double best, double cur) PbPick(World w, Coefficients c, double[] agg, double[] curCoord, double soldierForce, double coerce, double bourgeoisCoup, Polity dom, double domExcess, double pullWeight)
    {
        Polity attractor = w.Polity; double best = double.NegativeInfinity, cur = 0;
        foreach (var pol in Ideologies.Polities)
        {
            double s = Ideologies.SupportAt(agg, pol == w.Polity ? curCoord : Ideologies.BasePolityCoord(pol), c.IdeologyAffinitySlope);
            if (pol == Polity.MilitaryDictatorship) s += Politics.CoupSupport(w, c, soldierForce, coerce);
            if (pol == dom) s += pullWeight * domExcess;
            if (pol == Polity.BourgeoisDictatorship) s += bourgeoisCoup;
            if (c.RegimeTrackRecordWeight > 0)
                s *= Math.Max(0.05, 1.0 + c.RegimeTrackRecordWeight * w.RegimeTrackRecord[CrownParliament.RegimeFamily(pol)]);
            if (pol == w.Polity) cur = s;
            if (s > best) { best = s; attractor = pol; }
        }
        return (attractor, best, cur);
    }

    /// <summary>★(384) Politics.Transition の引力点を求めた直後（一党支配の引力が非ゼロのとき）：引力あり・なしの引力点の違い。本体が求めた <paramref name="attractor"/>・<paramref name="best"/>・<paramref name="cur"/> と複製を突き合わせる。</summary>
    internal static void PbDominanceCf(World w, Coefficients c, double[] agg, double[] curCoord, double soldierForce, double coerce, double bourgeoisCoup, Polity dom, double domExcess, Polity attractor, double best, double cur)
    {
        var (aWith, bWith, cWith) = PbPick(w, c, agg, curCoord, soldierForce, coerce, bourgeoisCoup, dom, domExcess, c.DominanceCoupWeight);
        var (aNo, _, _) = PbPick(w, c, agg, curCoord, soldierForce, coerce, bourgeoisCoup, dom, domExcess, 0.0);
        PbDominance(w, flip: aWith != aNo, decisive: aWith != aNo && aWith == dom && aWith != w.Polity && bWith - cWith > c.PolityInertia * w.Legitimacy, mismatch: aWith != attractor || bWith != best || cWith != cur, target: dom);
    }

    /// <summary>★(384) Politics.ExecuteRegimeChange の呼び。</summary>
    internal static void PbRegime(World w, bool violent, Polity target)
    {
        var a = PbOf(w); a.Regime += 1;
        if (violent) a.RegimeViolent += 1;
        if (target is Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship or Polity.MilitaryDictatorship) a.RegimeDict += 1;
        if (a.PullTarget is { } t && t == target) { a.DomExec += 1; a.PullTarget = null; }
    }

    /// <summary>★(384) Government.Move：非議会政体の歩幅に勅令の速さ（AutocraticPolicyAgility）が掛かった歩の数。</summary>
    internal static void PbAgility(World w) => PbOf(w).Agil += 1;

    /// <summary>★(384) World.Seize：資本家 POP の闘争度が動いた接収の数（SeizureMilitancyCost の読み手が走った回数）。</summary>
    internal static void PbSeize(World w, double share) { if (share > 0) PbOf(w).Seize += 1; }

    /// <summary>★(384) Politics.Transition の先頭：前の呼びの〈決定的〉の記録を空にする。</summary>
    internal static void PbPullReset(World w) => PbOf(w).PullTarget = null;
}
