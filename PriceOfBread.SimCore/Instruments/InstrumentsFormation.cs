namespace PriceOfBread.SimCore;

// (1201) c120fm 計器 formation_s：統一国家の成立（Formations.Check の名前の付け替えが真のとき）を 1 行ずつ書く。
// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（持ち物は計器の ConditionalWeakTable）。
// 州の数と従属国は計器が自分で数える（州の名は国の World.Regions・従属は Diplomacy.IsDependentOf の素の有無＝Formations.Owns を呼ばない）。
// 時は month（Diplomacy の月の通し番号・Check は四半期の頭の月）と year だけ（四半期の番号は読み手が month から出す＝計器に裸の暦の換算を置かない・CalendarTests）。
// 民族主義の取り分は ideology_s と同じ Ideologies.National の [4]。unificationCandidate は宣戦の候補の規則の名（付け替えの直後・段の書き換えの前の盤面）。
public static partial class Instruments
{
    internal const string FormationHeaderText = "month\tyear\tbaseName\tnewName\ttier\truleSource\townRegionsCount\tdepRegionsCount\tusedDependents\tnatShare\tunificationCandidate";

    sealed class FormationState { public readonly System.Collections.Generic.List<string> Rows = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, FormationState> FormationStates = new();

    /// <summary>付け替えの直後に 1 行を貯める（Formations.Check から・Instruments.On のときだけ）。</summary>
    internal static void FormationRow(Diplomacy d, Power power, FormationRule rule)
    {
        if (power.Nation is not { } w) return;
        var deps = new System.Collections.Generic.List<Power>();
        foreach (var q in d.Powers) if (q != power && d.IsDependentOf(power, q)) deps.Add(q);
        int depRegions = 0;
        foreach (var q in deps) if (q.Nation is { } qw) depRegions += qw.Regions.Count;
        var named = new System.Collections.Generic.List<string>(rule.RequiredAll);
        if (rule.AnyOfSet is not null) named.AddRange(rule.AnyOfSet);
        var used = new System.Collections.Generic.List<string>();
        foreach (var q in deps)
        {
            if (q.Nation is not { } qw) continue;
            bool hit = false;
            foreach (var name in named)
            {
                if (w.Regions.Exists(r => r.Name == name)) continue;   // 自分が直接持つ州は従属国を使っていない
                if (qw.Regions.Exists(r => r.Name == name)) { hit = true; break; }
            }
            if (hit) used.Add(NameOf(q));
        }
        string source = System.Array.IndexOf(Formations.Rules, rule) >= 0 ? "Rules"
            : rule.Tier == CultureRealms.TierA ? "CultureRealmA" : "CultureRealmB";
        double share = Ideologies.National(w)[4];
        string cand = Formations.UnificationCandidate(d, power, w, w.Coef)?.NewName ?? "";
        var cp = CpOf(d);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        FormationStates.GetValue(d, _ => new FormationState()).Rows.Add(string.Join('\t',
            I(cp.Month), I(w.Year), NameOf(power), rule.NewName, I(rule.Tier), source,
            I(w.Regions.Count), I(depRegions), string.Join('|', used),
            share.ToString("R", System.Globalization.CultureInfo.InvariantCulture), cand));
    }

    static void FormationTable(Sink sk, Diplomacy d)
    {
        using var wr = Open("formation", sk.Seed);
        wr.WriteLine(FormationHeaderText);
        foreach (var line in FormationRowsPending(d)) wr.WriteLine(line);
    }

    /// <summary>貯めた行の文字列の一覧（表に書く行そのもの・試験が読む）。</summary>
    internal static System.Collections.Generic.List<string> FormationRowsPending(Diplomacy d)
        => FormationStates.TryGetValue(d, out var st) ? new(st.Rows) : new();
}
