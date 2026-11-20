namespace PriceOfBread.SimCore;

// (1230) c124nr・(1249) c127nr：民族主義の根と統一の宣戦（旗 NationalismFromDispersion・UnificationWarWeary）の恒等の計器。事前登録 measurements/m1249_20261120_prereg_nr.txt。
//   (1249) formation2_s の maxX・fX は形 A′（X の人口が最大の 1 国）・maxXMain・fXMain は形 A（主の文化が X の国の中の最大）。natCulture は記録（旗 C は入れていない）。
//   (1249) tensionwhy_s（四半期の末・列強〔RankOf＝Great〕を含む組で緊張 1 以上）：緊張の終値と理由ごとの内訳（Diplomacy.TensionBreakdown＝読むだけの公開の口）。記録用＝門に使わない。
//   formation2_s（四半期の末・国×統一の規則〔UnificationV1〕）：必要州の直接と従属の数・国の全体と規則の文化の州だけの民族主義の取り分・文化の f（最大の国の X と世界の X）・攻め手の厭戦。
//   unifwar_s（統一の宣戦の可否の評価ごと＝Formations.UnificationWarBlock の口）：旧の規則（標的・交戦・同盟）と新の規則（＋攻め手の厭戦）の判定を同じ盤面で計器が自分で組み、エンジンの判定と並べる。
//   計器は Formations と Ideologies の関数を呼ばない（州の札・持ち主・POP・欄を自分で数える）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
public static partial class Instruments
{
    internal const string Formation2HeaderText = "year\tq\tnation\trule\truleTier\tformationTier\townDirect\townDep\tneed"
        + "\tnatAll\tnatCulture\tweightAll\tweightCulture\tculture\tmaxX\tworldX\tfX\texhaust\twearyNew\tmaxXMain\tfXMain";
    internal static readonly string TensionWhyHeaderText = "year\tq\tnationA\tnationB\trankA\trankB\ttension\t" + string.Join("\t", System.Enum.GetNames<TensionReason>());
    internal const string UnifWarHeaderText = "year\tq\tattacker\ttarget\tplayerFlag\texA\texT\tthreshold\ttargetIn\tatWar\tallied\toldCode\tnewCode\tengineCode";

    sealed class UnifState
    {
        public System.IO.StreamWriter? Wf, Wu, Wt;
        public readonly System.Collections.Generic.List<string> Pending = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, UnifState> UnifStates = new();

    /// <summary>統一の宣戦の可否の評価 1 件（Formations.UnificationWarBlock の中・Instruments.On の時だけ）。code＝エンジンの判定（UnificationBlock の値）。</summary>
    internal static void UnifWarEval(Diplomacy d, Coefficients c, Power p, Power t, System.Collections.Generic.List<Power> targets, int code)
    {
        if (!Tg.Cede) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!Wants("unifwar_s") || p.Nation is not { } pw || pw.WarmupFrozen) return;
        bool tin = false; foreach (var x in targets) if (ReferenceEquals(x, t)) { tin = true; break; }
        bool war = d.IsAtWar(p, t), ally = d.HasAlliance(p, t);
        int oldCode = !tin ? 1 : war ? 2 : ally ? 3 : 0;
        int newCode = oldCode == 0 && p.WarExhaustion > c.WarWearyThreshold ? 4 : oldCode;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sb = new System.Text.StringBuilder();
        sb.Append(I(pw.Year)).Append("\t{Q}\t").Append(NameOf(p)).Append('\t').Append(NameOf(t)).Append('\t').Append(p.IsPlayer ? "1" : "0")
          .Append('\t').Append(FR(p.WarExhaustion)).Append('\t').Append(FR(t.WarExhaustion)).Append('\t').Append(FR(c.WarWearyThreshold))
          .Append('\t').Append(tin ? "1" : "0").Append('\t').Append(war ? "1" : "0").Append('\t').Append(ally ? "1" : "0")
          .Append('\t').Append(I(oldCode)).Append('\t').Append(I(newCode)).Append('\t').Append(I(code));
        UnifStates.GetValue(d, _ => new UnifState()).Pending.Add(sb.ToString());
    }

    static void UnifTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.Cede) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        bool wf = Wants("formation2_s"), wu = Wants("unifwar_s"), wtw = Wants("tensionwhy_s");
        if ((!wf && !wu && !wtw) || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var d = sim.Diplomacy;
        var st = UnifStates.GetValue(d, _ => new UnifState());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string qs = I(q);
        if (wu)
        {
            if (st.Wu is null) { st.Wu = Open("unifwar", seed); st.Wu.WriteLine(UnifWarHeaderText); }
            foreach (var row in st.Pending) st.Wu.WriteLine(row.Replace("{Q}", qs));
        }
        st.Pending.Clear();
        if (wtw) TensionWhyRows(st, d, seed, qs);
        if (!wf) return;
        if (st.Wf is null) { st.Wf = Open("formation2", seed); st.Wf.WriteLine(Formation2HeaderText); }
        // 文化ごとの世界の X と、主の文化が X の国のうち最大の国の X（計器が州の札と POP を自分で足す）
        var world = new System.Collections.Generic.Dictionary<string, double>();
        var largest = new System.Collections.Generic.Dictionary<string, double>();
        var largestMain = new System.Collections.Generic.Dictionary<string, double>();
        var own = new System.Collections.Generic.Dictionary<string, double>();
        foreach (var n in sim.Nations)
        {
            own.Clear();
            foreach (var r in n.World.Regions)
            {
                if (r.Culture.Length == 0) continue;
                double pop = 0; foreach (var pp in r.Pops) pop += pp.Size;
                if (!(pop > 0)) continue;
                own[r.Culture] = own.GetValueOrDefault(r.Culture) + pop;
                world[r.Culture] = world.GetValueOrDefault(r.Culture) + pop;
            }
            foreach (var kv in own) if (kv.Value > largest.GetValueOrDefault(kv.Key)) largest[kv.Key] = kv.Value;   // 形 A′
            string main = n.World.SelfPower?.Culture ?? "";
            if (main.Length > 0 && own.TryGetValue(main, out double m) && m > largestMain.GetValueOrDefault(main)) largestMain[main] = m;   // 形 A
        }
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.SelfPower is not { } p) continue;
            var c = w.Coef;
            foreach (var rule in Formations.Rules)
            {
                if (System.Array.IndexOf(Formations.UnificationV1, rule.NewName) < 0 || rule.Tier <= p.FormationTier) continue;
                bool elig = (rule.BaseNames is not null && System.Array.IndexOf(rule.BaseNames, p.BaseName) >= 0) || (rule.CultureIs is not null && rule.CultureIs == p.Culture);
                if (!elig || (rule.ExcludeBaseNames is not null && System.Array.IndexOf(rule.ExcludeBaseNames, p.BaseName) >= 0)) continue;
                int direct = 0, dep = 0;
                foreach (var name in rule.RequiredAll)
                {
                    bool hasD = false; foreach (var r in w.Regions) if (r.Name == name) { hasD = true; break; }
                    if (hasD) { direct++; continue; }
                    foreach (var q2 in d.DependentsOf(p))
                    {
                        if (q2.Nation is not { } dw) continue;
                        bool hasQ = false; foreach (var r in dw.Regions) if (r.Name == name) { hasQ = true; break; }
                        if (hasQ) { dep++; break; }
                    }
                }
                string cul = rule.CultureIs ?? p.Culture;
                double wa = 0, na = 0, wc = 0, nc = 0;
                foreach (var r in w.Regions)
                {
                    bool inCul = r.Culture == cul;
                    foreach (var pp in r.Pops)
                    {
                        double voice = c.IdeologyVoiceFloor + (1.0 - c.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(pp, w.Suffrage, c);
                        double wt = pp.Size * pp.Consciousness * voice;
                        if (!(wt > 0)) continue;
                        double s4 = pp.IdeologyShares[(int)Ideology.Nationalism];
                        wa += wt; na += s4 * wt;
                        if (inCul) { wc += wt; nc += s4 * wt; }
                    }
                }
                double wx = world.GetValueOrDefault(cul), mx = largest.GetValueOrDefault(cul), mxm = largestMain.GetValueOrDefault(cul);
                sb.Clear();
                sb.Append(I(w.Year)).Append('\t').Append(qs).Append('\t').Append(NameOf(w)).Append('\t').Append(rule.NewName)
                  .Append('\t').Append(I(rule.Tier)).Append('\t').Append(I(p.FormationTier)).Append('\t').Append(I(direct)).Append('\t').Append(I(dep)).Append('\t').Append(I(rule.RequiredAll.Length))
                  .Append('\t').Append(FR(wa > 1e-9 ? na / wa : 0)).Append('\t').Append(FR(wc > 1e-9 ? nc / wc : 0)).Append('\t').Append(FR(wa)).Append('\t').Append(FR(wc))
                  .Append('\t').Append(cul).Append('\t').Append(FR(mx)).Append('\t').Append(FR(wx)).Append('\t').Append(FR(wx > 0 ? 1.0 - mx / wx : double.NaN))
                  .Append('\t').Append(FR(p.WarExhaustion)).Append('\t').Append(p.WarExhaustion > c.WarWearyThreshold ? "1" : "0")
                  .Append('\t').Append(FR(mxm)).Append('\t').Append(FR(wx > 0 ? 1.0 - mxm / wx : double.NaN));
                st.Wf.WriteLine(sb.ToString());
            }
        }
    }

    /// <summary>(1249) tensionwhy_s の行（列強を含む組・緊張 1 以上）。内訳は TensionBreakdown（理由ごとの寄与・合計は Tension に一致）。</summary>
    static void TensionWhyRows(UnifState st, Diplomacy d, ulong seed, string qs)
    {
        if (st.Wt is null) { st.Wt = Open("tensionwhy", seed); st.Wt.WriteLine(TensionWhyHeaderText); }
        int nr = System.Enum.GetValues<TensionReason>().Length;
        var vals = new double[nr];
        var sb = new System.Text.StringBuilder();
        var ps = d.Powers;
        for (int i = 0; i < ps.Count; i++)
            for (int j = i + 1; j < ps.Count; j++)
            {
                Power a = ps[i], b = ps[j];
                if (a.Nation is not { } wa || b.Nation is null) continue;
                PowerRank ra = d.RankOf(a), rb = d.RankOf(b);
                if (ra != PowerRank.Great && rb != PowerRank.Great) continue;
                double t = d.Tension(a, b);
                if (!(t >= 1.0)) continue;
                System.Array.Clear(vals);
                foreach (var (reason, v) in d.TensionBreakdown(a, b)) vals[(int)reason] = v;
                sb.Clear();
                sb.Append(wa.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(qs).Append('\t').Append(NameOf(a)).Append('\t').Append(NameOf(b))
                  .Append('\t').Append(((int)ra).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(((int)rb).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(FR(t));
                foreach (var v in vals) sb.Append('\t').Append(FR(v));
                st.Wt.WriteLine(sb.ToString());
            }
    }

    static void UnifClose(MultiNationSim sim)
    {
        if (!Tg.Cede) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!UnifStates.TryGetValue(sim.Diplomacy, out var st)) return;
        st.Wf?.Dispose(); st.Wf = null; st.Wu?.Dispose(); st.Wu = null; st.Wt?.Dispose(); st.Wt = null; st.Pending.Clear();
    }
}
