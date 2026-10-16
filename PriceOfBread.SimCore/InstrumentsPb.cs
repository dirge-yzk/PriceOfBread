namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (384) 政治の束の計器（読むだけ・事前登録 measurements/pb_20261017_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsScalars.cs／InstrumentsFm.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・World に状態を足さない
// （積む先は World ごとの ConditionalWeakTable＝セーブに載らない）。呼ぶ側は `if (Instruments.On)` で守る＝既定は 1 命令も走らない。scalars_s の末尾の列が読む。
//   pbDomActive ＝ 一党支配の引力（DominanceCoupWeight×議席の過剰）が非ゼロだった Politics.Transition の呼びの数（その World の累計）。
//   pbDomFlip   ＝ そのうち、引力あり・なしで引力点が違った呼びの数。
//   pbDomDecisive ＝ そのうち、引力ありの引力点が一党支配の独裁形態で、現政体と違い、第 1 の門（支持差が保持力 PolityInertia×正統性 を超える）を越えた呼びの数。以降の門（動員が保持力を超える等）は含まない＝上限。
//   pbDomExec   ＝ そのうち、同じ呼びの中で ExecuteRegimeChange が引力の独裁形態を目標に実行された数（Transition の後ろの門を全て越えた＝引力が実際に政体を変えた上限に近い数。同じ四半期の別の経路が同じ目標へ実行しても数える）。
//   pbDomMismatch ＝ 計器の複製（Politics.PickAttractor）が本体のループと違う引力点・支持を出した呼びの数（0 でなければ複製がずれている＝計器の信頼を落とす）。
//   pbRegime／pbRegimeViolent／pbRegimeDict ＝ Politics.ExecuteRegimeChange の呼びの数／そのうち暴力の転換（violent）の数／そのうち目標が独裁（BD・PD・MD）の数。
// ⚠限界（計器が自分で申告する）：累計は World ごと＝分離独立で生まれた国は 0 から数え直す（変名しても World は同じ）。引力の反事実は〈引力だけを 0 にした〉1 呼びの反事実で、引力が無い世界の軌道ではない。
public static partial class Instruments
{
    internal sealed class PbAcc { public double DomActive, DomFlip, DomDecisive, DomExec, DomMismatch, Regime, RegimeViolent, RegimeDict; public Polity? PullTarget; }
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

    /// <summary>★(384) Politics.ExecuteRegimeChange の呼び。</summary>
    internal static void PbRegime(World w, bool violent, Polity target)
    {
        var a = PbOf(w); a.Regime += 1;
        if (violent) a.RegimeViolent += 1;
        if (target is Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship or Polity.MilitaryDictatorship) a.RegimeDict += 1;
        if (a.PullTarget is { } t && t == target) { a.DomExec += 1; a.PullTarget = null; }
    }

    /// <summary>★(384) Politics.Transition の先頭：前の呼びの〈決定的〉の記録を空にする。</summary>
    internal static void PbPullReset(World w) => PbOf(w).PullTarget = null;
}
