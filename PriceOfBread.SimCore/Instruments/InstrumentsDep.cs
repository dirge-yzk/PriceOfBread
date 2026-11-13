namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（持ち物は計器の ConditionalWeakTable）。〔履歴 InstrumentsDep.cs#1〕
public static partial class Instruments
{
    internal const string DepHeaderText = "kindCode\tmonth\tyear\tcallerMember\tcallerLine\tsuzerain\tdependent\tpriorSuzerainOfDependent\tsuzerainOfSuzerainBefore\tchainLenIndex\tcycleFlag\tcapHitFlag\tcutSuzerain\tatWarFlag\tlinksBeforeCount\tlinksAfterCount\tcyclesBeforeCount\tcyclesAfterCount";

    /// <summary>張る前の断面（呼び口の局所に持たせる）。</summary>
    internal sealed class DepPre
    {
        public Power Suz = null!, Dep = null!; public Power? PriorSuz, SuzOfSuz;
        public int ChainLen, Links, Cycles; public bool Cycle, CapHit, AtWar;
    }

    sealed class DepState { public readonly System.Collections.Generic.List<string> Rows = new(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, DepState> DepStates = new();

    /// <summary>計器自身の親の表（国→宗主）とリンクの数。IsDependentOf の素の有無だけを読む。</summary>
    static System.Collections.Generic.Dictionary<Power, Power> DepParents(Diplomacy d, out int links)
    {
        var par = new System.Collections.Generic.Dictionary<Power, Power>();
        links = 0;
        foreach (var dep in d.Powers)
            foreach (var s in d.Powers)
                if (s != dep && d.IsDependentOf(s, dep)) { links++; par.TryAdd(dep, s); }
        return par;
    }

    /// <summary>親の表の輪の数（各国の親は高々 1 つ＝関数のグラフの輪）。</summary>
    static int DepCycles(Diplomacy d, System.Collections.Generic.Dictionary<Power, Power> par)
    {
        var state = new System.Collections.Generic.Dictionary<Power, int>();   // 1＝今の道の上・2＝済み
        int cycles = 0;
        foreach (var start in d.Powers)
        {
            if (state.ContainsKey(start)) continue;
            var path = new System.Collections.Generic.List<Power>();
            Power? x = start;
            while (x is not null && !state.ContainsKey(x)) { state[x] = 1; path.Add(x); x = par.TryGetValue(x, out var p) ? p : null; }
            if (x is not null && state[x] == 1) cycles++;   // 今の道の上に戻った＝新しい輪
            foreach (var y in path) state[y] = 2;
        }
        return cycles;
    }

    /// <summary>SetDependency の頭（張る前）：断面を控える。</summary>
    internal static DepPre DepBefore(Diplomacy d, Power suzerain, Power dependent)
    {
        var par = DepParents(d, out int links);
        var pre = new DepPre { Suz = suzerain, Dep = dependent, Links = links, Cycles = DepCycles(d, par) };
        pre.PriorSuz = par.TryGetValue(dependent, out var ps) ? ps : null;
        pre.SuzOfSuz = par.TryGetValue(suzerain, out var ss) ? ss : null;
        int n = d.Powers.Count, steps = 0;
        Power? up = pre.SuzOfSuz;
        while (up is not null)
        {
            if (steps >= n) { pre.CapHit = true; break; }
            steps++;
            if (up == dependent) { pre.Cycle = true; break; }
            up = par.TryGetValue(up, out var u) ? u : null;
        }
        pre.ChainLen = steps;
        foreach (var w in d.Wars)
            if ((w.A == suzerain && w.B == dependent) || (w.A == dependent && w.B == suzerain)) { pre.AtWar = true; break; }
        return pre;
    }

    /// <summary>SetDependency の尻（張った後）：1 行を貯める。</summary>
    internal static void DepAfter(Diplomacy d, DepPre pre, string callerMember, int callerLine)
    {
        var par = DepParents(d, out int links);
        string cut = pre.SuzOfSuz is { } ss && !d.IsDependentOf(ss, pre.Suz) ? NameOf(ss) : "";
        var cp = CpOf(d);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        DepStates.GetValue(d, _ => new DepState()).Rows.Add(string.Join('\t',
            "1", I(cp.Month), I(cp.Year), callerMember, I(callerLine), NameOf(pre.Suz), NameOf(pre.Dep),
            pre.PriorSuz is { } p ? NameOf(p) : "", pre.SuzOfSuz is { } s2 ? NameOf(s2) : "",
            I(pre.ChainLen), pre.Cycle ? "1" : "0", pre.CapHit ? "1" : "0", cut, pre.AtWar ? "1" : "0",
            I(pre.Links), I(links), I(pre.Cycles), I(DepCycles(d, par))));
    }

    /// <summary>試合の終わりの全リンクの行（kindCode 2）。</summary>
    internal static string DepFinalRow(Diplomacy d)
    {
        var par = DepParents(d, out int links);
        var cp = CpOf(d);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Join('\t', "2", I(cp.Month), I(cp.Year), "", "", "", "", "", "", "", "", "", "", "", "", I(links), "", I(DepCycles(d, par)));
    }

    static void DepTable(Sink sk, Diplomacy d)
    {
        using var wr = Open("dep", sk.Seed);
        wr.WriteLine(DepHeaderText);
        foreach (var line in DepRowsPending(d)) wr.WriteLine(line);
        wr.WriteLine(DepFinalRow(d));
    }

    /// <summary>呼びの行の文字列の一覧（表に書く行そのもの・試験が読む）。</summary>
    internal static System.Collections.Generic.List<string> DepRowsPending(Diplomacy d)
        => DepStates.TryGetValue(d, out var st) ? new(st.Rows) : new();
}
