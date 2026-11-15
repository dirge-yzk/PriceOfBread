using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// (1145) c112hy 高い帯の冷め方の枝の恒等の計器 dipcool_s（試合×四半期・1 行）。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。
// 外交の月の DriftTension の頭で、緊張の表・従属の表・遺恨の表・国の敗戦の衝撃を計器が自分で読み（DefeatedShock・CannotDeclareWar・CoolSlot は呼ばない）、
// 同じ盤面で〈旧の規則＝ハイエナの枝あり（率は旧の既定 2.0）〉と〈新の規則＝ハイエナの枝なし〉の 2 つの冷めた後の値を自分で組む。
// 緊張の走査の直後（遺恨の風化の前）に、エンジンが実際に書いた値を同じ鍵で読み、どちらの規則に合ったかを数える。
public static partial class Instruments
{
    internal const string DipcoolHeaderText = "quarter\tyear\tmonths\tlowRows\thighRows\tfrozenRows\toldHyenaRows\tfreshRows\toldHyenaFreshRows\t"
        + "obsHyenaRows\tobsHyenaFreshRows\tobsBothRows\tobsOtherRows\tgrudgeRows\tpeaceRows\t"
        + "maxAbsDiffNewGrudge\tmaxAbsDiffNewHigh\tmaxAbsDiffOldHigh\tmaxAbsDiffFrozen\tmaxAbsDiffLow\t"
        + "sumObsCoolGrudge\tsumOldCoolGrudge\tsumNewCoolGrudge\tsumObsCoolFresh\tsumOldCoolFresh\tsumNewCoolFresh\tsumObsCoolFrozen\tsumFrozenRuleCool";

    /// <summary>旧の規則の率（p06 の DefeatShockTensionRelief の旧の既定・月×衝撃）。係数が 0 の木でも旧の規則を同じ盤面で数えるため、計器が自分で持つ。</summary>
    internal const double DipcoolOldRelief = 2.0;
    /// <summary>旧の規則の保護が効く衝撃の下限（係数 DefeatShockProtectMin の削除前の既定 0.3・(1154) で係数ごと消した＝計器が自分の定数で持つ・列の値は変えない）。</summary>
    internal const double DipcoolProtectMin = 0.3;
    /// <summary>規則に合ったと数える差の許し（緊張の単位）。</summary>
    internal const double DipcoolMatchTol = 1e-9;

    /// <summary>1 つの組の頭の断面と、2 つの規則の冷めた後の値（純関数で組む）。</summary>
    internal readonly record struct DipcoolRow(long Key, double T, double Grudge, double Shock, bool Frozen, int OldBranch, double OldAfter, double NewAfter);

    internal sealed class DipcoolPre { public readonly List<DipcoolRow> Rows = new(); }

    sealed class DipcoolAcc
    {
        public int Months, Low, High, Frozen, OldHy, Fresh, OldHyFresh, ObsHy, ObsHyFresh, ObsBoth, ObsOther, Grudge, Peace;
        public double MaxNewGrudge, MaxNewHigh, MaxOldHigh, MaxFrozen, MaxLow;
        public double ObsG, OldG, NewG, ObsF, OldF, NewF, ObsFrozen, RuleFrozen;
    }
    sealed class DipcoolSink { public DipcoolAcc Acc = new(); public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, DipcoolSink> DipcoolSinks = new();

    /// <summary>枝の番号：0＝低い帯・1＝凍結（従属）・2＝ハイエナ・3＝遺恨（遺恨＞0）・4＝平時（遺恨 0）。</summary>
    internal static (int Branch, double After) DipcoolRule(double t, double grudge, double shock, bool frozen, bool hyena, double band, double decay, double frozenRate, double protectMin)
    {
        if (t < band) return (0, System.Math.Max(0, t - decay));
        if (frozen) return (1, System.Math.Max(0, t - frozenRate));
        if (hyena && shock >= protectMin) return (2, System.Math.Max(0, t - DipcoolOldRelief * shock));
        double g = !(grudge > 0) ? 0 : grudge > 1 ? 1 : grudge;
        double rate = decay * (1.0 - g);
        return (g > 0 ? 3 : 4, rate > 0 ? System.Math.Max(0, t - rate) : t);
    }

    /// <summary>DriftTension の頭：全ての鍵の断面と 2 つの規則の値。衝撃は国の DefeatShock を Id の最初の力から読む。</summary>
    internal static DipcoolPre? DipcoolBefore(Diplomacy d, Coefficients c, Dictionary<long, double> tension, HashSet<long> dependency)
    {
        foreach (var p in d.Powers) if (p.Nation is { WarmupFrozen: true }) return null;   // 助走の月は数えない
        var shock = new Dictionary<int, double>();
        foreach (var p in d.Powers) shock.TryAdd(p.Id, p.Nation?.DefeatShock ?? 0);
        var pre = new DipcoolPre();
        foreach (var kv in tension)
        {
            long k = kv.Key; double t = kv.Value;
            int hi = (int)(k >> 32), lo = (int)(uint)k;
            bool frozen = shock.ContainsKey(hi) && shock.ContainsKey(lo)
                && (dependency.Contains(((long)hi << 32) | (uint)lo) || dependency.Contains(((long)lo << 32) | (uint)hi));
            double sh = System.Math.Max(shock.GetValueOrDefault(hi), shock.GetValueOrDefault(lo));
            double g = d._grudge.GetValueOrDefault(k);
            var o = DipcoolRule(t, g, sh, frozen, true, c.HighTensionBand, c.TensionDecay, c.FrozenTensionDecayRate, DipcoolProtectMin);
            var n = DipcoolRule(t, g, sh, frozen, false, c.HighTensionBand, c.TensionDecay, c.FrozenTensionDecayRate, DipcoolProtectMin);
            pre.Rows.Add(new DipcoolRow(k, t, g, sh, frozen, o.Branch, o.After, n.After));
        }
        return pre;
    }

    /// <summary>緊張の走査の直後：エンジンが書いた値を読み、四半期の数へ積む。</summary>
    internal static void DipcoolAfter(Diplomacy d, Coefficients c, Dictionary<long, double> tension, DipcoolPre pre)
    {
        var a = DipcoolSinks.GetValue(d, _ => new DipcoolSink()).Acc;
        a.Months++;
        foreach (var r in pre.Rows)
        {
            double after = tension.GetValueOrDefault(r.Key);
            double dOld = System.Math.Abs(after - r.OldAfter), dNew = System.Math.Abs(after - r.NewAfter);
            if (r.OldBranch == 0) { a.Low++; if (dNew > a.MaxLow) a.MaxLow = dNew; continue; }
            a.High++;
            if (r.OldBranch == 1)
            {
                a.Frozen++; if (dNew > a.MaxFrozen) a.MaxFrozen = dNew;
                a.ObsFrozen += r.T - after; a.RuleFrozen += r.T - r.NewAfter;
                continue;
            }
            if (dNew > a.MaxNewHigh) a.MaxNewHigh = dNew;
            if (dOld > a.MaxOldHigh) a.MaxOldHigh = dOld;
            bool mo = dOld <= DipcoolMatchTol, mn = dNew <= DipcoolMatchTol;
            bool fresh = r.Grudge >= 0.9;
            if (fresh) a.Fresh++;
            if (r.OldBranch == 2)
            {
                a.OldHy++; if (fresh) a.OldHyFresh++;
                if (mo && !mn) { a.ObsHy++; if (fresh) a.ObsHyFresh++; }
            }
            if (mo && mn) a.ObsBoth++;
            else if (!mo && !mn) a.ObsOther++;
            if (r.Grudge > 0)
            {
                a.Grudge++;
                if (dNew > a.MaxNewGrudge) a.MaxNewGrudge = dNew;
                a.ObsG += r.T - after; a.OldG += r.T - r.OldAfter; a.NewG += r.T - r.NewAfter;
                if (fresh) { a.ObsF += r.T - after; a.OldF += r.T - r.OldAfter; a.NewF += r.T - r.NewAfter; }
            }
            else a.Peace++;
        }
    }

    static void DipcoolTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = DipcoolSinks.GetValue(sim.Diplomacy, _ => new DipcoolSink());
        var a = sk.Acc; sk.Acc = new DipcoolAcc();
        if (!Wants("dipcool_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        if (sk.W is null) { sk.W = Open("dipcool", seed); sk.W.WriteLine(DipcoolHeaderText); }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string I(int v) => v.ToString(inv);
        sk.W.WriteLine(string.Join('\t', I(q), I(sim.Nations[0].World.Year), I(a.Months), I(a.Low), I(a.High), I(a.Frozen), I(a.OldHy), I(a.Fresh), I(a.OldHyFresh),
            I(a.ObsHy), I(a.ObsHyFresh), I(a.ObsBoth), I(a.ObsOther), I(a.Grudge), I(a.Peace),
            F(a.MaxNewGrudge), F(a.MaxNewHigh), F(a.MaxOldHigh), F(a.MaxFrozen), F(a.MaxLow),
            F(a.ObsG), F(a.OldG), F(a.NewG), F(a.ObsF), F(a.OldF), F(a.NewF), F(a.ObsFrozen), F(a.RuleFrozen)));
    }

    static void DipcoolClose(MultiNationSim sim)
    {
        if (DipcoolSinks.TryGetValue(sim.Diplomacy, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
