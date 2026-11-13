namespace PriceOfBread.SimCore.Display;

/// <summary>選んだ法の賛否の行の種類（階級・議会の党・軍・国庫）。</summary>
public enum StakeKind { Class, Party, Army, Treasury }

/// <summary>選んだ法の賛否の 1 行。<paramref name="Next"/>・<paramref name="Prev"/>＝その一歩の押す力の割合（符号つき・全行の |K| の和で割る・正＝賛成）。段が無い向き・全員が無関心の向きは null。
/// <paramref name="People"/>・<paramref name="Voice"/> は階級の行だけ（他は NaN）。Voice は階級の和で 1。</summary>
public sealed record StakeLine(string Key, string Name, string Group, StakeKind Kind,
                               double People, double Voice, double? Next, double? Prev);

/// <summary>半円 1 つ（その一歩の賛成と反対の割合・和 1）。<paramref name="Caption"/>＝「今の段→その段」。</summary>
public sealed record StakeSplit(string Caption, double For, double Against);

/// <summary>選んだ法の賛否（盤面は読むだけ・状態を持たない）。行の和は議会の門の純支持（<see cref="PolicyPreferences.NetSupport(World, PolicyAxis, bool)"/>）と一致する。
/// 出どころの差し替え口は <see cref="Voices"/> と <see cref="ClassPref"/> の 2 本だけ。
/// 「得・損」とは名乗らない＝エンジンの好みは所得の変化でなく、賛成・反対とその押す力。</summary>
public static class LawStakes
{
    /// <summary>議会・軍・国庫の行の群の見出し。</summary>
    public const string OtherGroup = "議会と軍";

    /// <summary>階級ごとの影響力の素と人数と名と群とブロック。★差し替え口 1（今＝<see cref="InterestGroups"/> の POP ごとの重みの和）。</summary>
    internal readonly record struct ClassVoice(int ClassId, string Name, int Layer, int Bloc, double Weight, double People);

    private static readonly string[] PartyNames = ["保守党", "自由党", "社会党"];

    /// <summary>階級ごとの重みと人数（<see cref="InterestGroups.Blocs"/> と同じ走査・同じ重み）。人数 0 の階級は行にしない。並びは層・影響力の大きい順。</summary>
    internal static IReadOnlyList<ClassVoice> Voices(World w)
    {
        var k = InterestGroups.Context(w);
        var order = new List<(int Id, int Bloc)>();
        var agg = new Dictionary<(int, int), (string Name, int Layer, double Weight, double People)>();
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            var key = (p.ClassId, PolicyPreferences.BlocOf(p));
            double wt = InterestGroups.PopWeight(w, p, k);
            if (agg.TryGetValue(key, out var a)) agg[key] = (a.Name, a.Layer, a.Weight + wt, a.People + p.Size);
            else { agg[key] = (p.ClassName, PlayText.ClassLayerOf(p), wt, p.Size); order.Add(key); }
        }
        var list = new List<ClassVoice>(order.Count);
        foreach (var key in order)
        {
            var a = agg[(key.Id, key.Bloc)];
            if (!(a.People > 1e-9)) continue;
            list.Add(new ClassVoice(key.Id, a.Name, a.Layer, key.Bloc, a.Weight, a.People));
        }
        return list.Select((c, i) => (c, i)).OrderBy(t => t.c.Layer).ThenByDescending(t => t.c.Weight).ThenBy(t => t.i).Select(t => t.c).ToList();
    }

    /// <summary>階級の好み（進める向きなら <paramref name="increasing"/>＝真）。★差し替え口 2（今＝そのブロックの <see cref="PolicyPreferences.Pref"/>）。</summary>
    internal static double ClassPref(World w, in PolicyPreferences.Profile e, PolicyAxis a, in ClassVoice c, bool increasing)
        => PolicyPreferences.Pref(a, c.Bloc, e, w, increasing);

    /// <summary>その向きに動かす一歩の段があるか。</summary>
    private static bool HasStep(World w, LawDef def, bool increasing)
        => increasing ? def.Current(w) < def.LevelBase + def.Stages.Length - 1 : def.Current(w) > def.LevelBase;

    /// <summary>押す力 K の行（正＝賛成・負＝反対）。<see cref="PolicyPreferences.NetSupport(World, PolicyAxis, bool)"/> と同じ項・同じ係数・同じ足し順＝和が一致する。
    /// 階級の行は <paramref name="voices"/> と同じ並び・続けて党・軍・国庫。</summary>
    private readonly record struct Push(StakeKind Kind, string Name, int Index, double K);
    private sealed record PushSet(List<Push> Rows, double Abs);

    private static PushSet Pushes(World w, LawDef def, IReadOnlyList<ClassVoice> voices, in PolicyPreferences.Profile e, bool increasing)
    {
        var a = def.Axis;
        double sign = increasing ? 1 : -1;
        double street = w.Coef.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && PolicyPreferences.HasLegislature(w.Polity)
            ? 1.0 - PoliticalQuantities.Clamp01(w.Coef.ParliamentStreetSupersede) : 1.0;
        var rows = new List<Push>(voices.Count + 5);
        for (int i = 0; i < voices.Count; i++)
            rows.Add(new(StakeKind.Class, voices[i].Name, i, street * voices[i].Weight * ClassPref(w, e, a, voices[i], increasing) * sign));
        if (w.ParliamentEnabled && PolicyPreferences.HasLegislature(w.Polity))
            for (int party = 0; party < PolicyPreferences.BlocCount; party++)
                rows.Add(new(StakeKind.Party, PartyNames[party], party,
                    w.Coef.ParliamentLawWeight * w.PartySeats[party] * PolicyPreferences.Pref(a, party, e, w, increasing) * sign));
        rows.Add(new(StakeKind.Army, "軍", 0, PolicyPreferences.ArmyPower(w) * PolicyPreferences.ArmyStake(a) * sign));
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax)
            rows.Add(new(StakeKind.Treasury, "国庫", 0, e.StateRevenue * sign));
        double abs = 0;
        foreach (var r in rows) abs += Math.Abs(r.K);
        return new PushSet(rows, abs);
    }

    /// <summary>選んだ法の賛否の行（階級・党・軍・国庫）。無関心で 0 の党・軍・国庫の行は出さない。次の段・前の段が無い向きの列は null。</summary>
    public static IReadOnlyList<StakeLine> Lines(World w, LawDef def)
    {
        var e = PolicyPreferences.Build(w);
        var voices = Voices(w);
        double total = 0;
        foreach (var v in voices) total += v.Weight;
        PushSet? next = HasStep(w, def, true) ? Pushes(w, def, voices, e, true) : null;
        PushSet? prev = HasStep(w, def, false) ? Pushes(w, def, voices, e, false) : null;
        var basis = next ?? prev;
        if (basis is null) return [];
        static double? Share(PushSet? set, int row) => set is { Abs: > 0 } s ? s.Rows[row].K / s.Abs : null;

        var lines = new List<StakeLine>();
        for (int row = 0; row < basis.Rows.Count; row++)
        {
            var (kind, name, index, _) = basis.Rows[row];
            if (kind != StakeKind.Class)
            {
                double nk = next?.Rows[row].K ?? 0, pk = prev?.Rows[row].K ?? 0;
                if (!(Math.Abs(nk) > 1e-9) && !(Math.Abs(pk) > 1e-9)) continue;   // 無関心の党・軍の行は出さない
            }
            if (kind == StakeKind.Class)
            {
                var v = voices[index];
                lines.Add(new StakeLine($"class.{v.ClassId}.{v.Bloc}", name, PlayText.ClassLayerNames[v.Layer], kind,
                    v.People, total > 1e-12 ? v.Weight / total : 0, Share(next, row), Share(prev, row)));
            }
            else
                lines.Add(new StakeLine($"{kind}.{index}", name, OtherGroup, kind, double.NaN, double.NaN, Share(next, row), Share(prev, row)));
        }
        return lines;
    }

    /// <summary>半円 1 つ（その向きの賛成と反対の割合）。段が無い向き・全員が無関心（押す力の絶対値の和が 0）の向きは null。</summary>
    public static StakeSplit? Split(World w, LawDef def, bool increasing)
    {
        if (!HasStep(w, def, increasing)) return null;
        var e = PolicyPreferences.Build(w);
        var (rows, abs) = Pushes(w, def, Voices(w), e, increasing);
        if (!(abs > 0)) return null;
        double pro = 0, con = 0;
        foreach (var r in rows) { if (r.K > 0) pro += r.K; else con -= r.K; }
        int cur = def.Current(w);
        return new StakeSplit($"{def.StageName(cur)}→{def.StageName(cur + (increasing ? 1 : -1))}", pro / abs, con / abs);
    }

    /// <summary>行の押す力の合計（試験が議会の門の純支持と突き合わせる）。向きに段が無くても求める。</summary>
    internal static double NetPush(World w, LawDef def, bool increasing)
    {
        var e = PolicyPreferences.Build(w);
        var (rows, _) = Pushes(w, def, Voices(w), e, increasing);
        double sum = 0;
        foreach (var r in rows) sum += r.K;
        return sum;
    }
}
