# r788 使い捨ての読むだけのプローブ（講和 1 回ごとに 月・戦争の添字・連鎖の根・崩壊の旗・賠償・州の前後）を木に入れる。
# 使い方: python patch_probe.py <木の根>   ＝ 書くだけの欄と計器の表 r788_s<seed>.tsv を足す（軌道は変えない）。
import sys, os
root = sys.argv[1]
core = os.path.join(root, 'PriceOfBread.SimCore')

def rep1(path, old, new):
    p = os.path.join(core, path)
    s = open(p, encoding='utf-8').read()
    n = s.count(old)
    assert n == 1, (path, n, old[:80])
    s = s.replace(old, new)
    open(p, 'w', encoding='utf-8', newline='').write(s)

D = 'Diplomacy.cs'
# 1) 開戦の登録（連鎖の根と段）
rep1(D, "        if (Instruments.On) Instruments.WarStarted(this, war, _year);   // 計器（読むだけ）\n",
        "        if (Instruments.On) Instruments.WarStarted(this, war, _year);   // 計器（読むだけ）\n        if (Instruments.On) Instruments.R788Reg(this, war, depth, R788Suz);\n")
# 2) 宗主の参戦の印
rep1(D, "        AddBelligerent(new War { A = suz, B = enemy }, c, depth: 1, visited: []);   // ★宗主の同盟も連鎖する",
        "        R788Suz = true; AddBelligerent(new War { A = suz, B = enemy }, c, depth: 1, visited: []); R788Suz = false;   // ★宗主の同盟も連鎖する")
# 3) 月
rep1(D, "        Cost[CostLedger.DiploPowerMonths] += Powers.Count;",
        "        if (Instruments.On) Instruments.R788Month(this, month, _year);\n        Cost[CostLedger.DiploPowerMonths] += Powers.Count;")
# 4) 講和の呼び出しの前後
rep1(D, "                ResolvePeace(c, war); EndWar(war, i, c);   // 終戦の記録は EndWar の中",
        "                var r788pre = Instruments.On ? Instruments.R788Before(war, aCollapse, bCollapse, decisive, weary, fronts) : null;\n"
        "                R788Hit = false;\n"
        "                ResolvePeace(c, war);\n"
        "                if (r788pre is not null) Instruments.R788After(this, war, r788pre, R788Hit, R788Rep, R788RepBase, R788Sa, R788Gdp, R788R, R788Captured);\n"
        "                EndWar(war, i, c);   // 終戦の記録は EndWar の中")
# 5) 賠償の値を書き留める
rep1(D, "        Log.Add($\"{_year} {winner.Name}が{loser.Name}に勝利（戦勝点{sa:0.00}）:",
        "        R788Hit = true; R788Rep = rep; R788RepBase = repBase; R788Sa = sa; R788Gdp = loser.GdpPerMonth.Value; R788R = loser.Nation?.FiscalFundingRatio.Value ?? double.NaN; R788Captured = captured;\n"
        "        Log.Add($\"{_year} {winner.Name}が{loser.Name}に勝利（戦勝点{sa:0.00}）:")
# 欄
rep1(D, "    private int _year;\n",
        "    private int _year;\n    internal bool R788Suz, R788Hit; internal double R788Rep, R788RepBase, R788Sa, R788Gdp, R788R; internal int R788Captured;   // r788 読むだけ\n")

# 計器の表を閉じる所
rep1('Instruments.cs', "        if (Dips.TryGetValue(sim.Diplomacy, out var dip)) WarTables(sk, dip);\n",
     "        if (Dips.TryGetValue(sim.Diplomacy, out var dip)) WarTables(sk, dip);\n        R788Table(sk, sim.Diplomacy);\n")

probe = r'''namespace PriceOfBread.SimCore;
// r788 使い捨て（読むだけ）：講和 1 回ごとの行。
public static partial class Instruments
{
    sealed class R788State
    {
        public int Month = -1, Year;
        public int Count;            // 登録の通し番号（＝ wars_s の行の添字と同じ順）
        public int CurRoot = -1;
        public readonly System.Runtime.CompilerServices.ConditionalWeakTable<War, int[]> Info = new();   // {idx, root, depth, suz, startMonth}
        public readonly System.Collections.Generic.List<string> Rows = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, R788State> R788S = new();
    internal sealed class R788Pre { public double TA, TB; public int[] RA = new int[3], RB = new int[3]; public double PA, PB, PcA, PcB; public bool ACol, BCol, Dec, Weary, Forced; public int FA, FB; public double Score, PushA, PushB; public double ExA, ExB; }

    internal static void R788Month(Diplomacy d, int month, int year) { var st = R788S.GetValue(d, _ => new R788State()); st.Month = month; st.Year = year; }
    internal static void R788Reg(Diplomacy d, War war, int depth, bool suz)
    {
        var st = R788S.GetValue(d, _ => new R788State());
        int idx = st.Count++;
        if (depth == 0) st.CurRoot = idx;
        st.Info.AddOrUpdate(war, [idx, st.CurRoot, depth, suz ? 1 : 0, st.Month]);
    }
    static void R788Count(Power p, int[] r, out double pop, out double popCore)
    {
        pop = 0; popCore = 0;
        if (p.Nation is not { } w) return;
        foreach (var g in w.Regions) { r[(int)g.Tier]++; double s = g.SumPopulation(); pop += s; if (g.Tier != RegionTier.Colonial) popCore += s; }
    }
    internal static R788Pre R788Before(War war, bool aCol, bool bCol, bool dec, bool weary, System.Collections.Generic.Dictionary<Power, int> fronts)
    {
        var x = new R788Pre { ACol = aCol, BCol = bCol, Dec = dec, Weary = weary, Forced = war.ForcedPeace, Score = war.Score, PushA = war.PushA, PushB = war.PushB, ExA = war.A.WarExhaustion, ExB = war.B.WarExhaustion };
        x.TA = war.A.Nation?.Treasury.Value ?? double.NaN; x.TB = war.B.Nation?.Treasury.Value ?? double.NaN;
        R788Count(war.A, x.RA, out x.PA, out x.PcA); R788Count(war.B, x.RB, out x.PB, out x.PcB);
        x.FA = fronts.GetValueOrDefault(war.A); x.FB = fronts.GetValueOrDefault(war.B);
        return x;
    }
    internal static void R788After(Diplomacy d, War war, R788Pre x, bool hit, double rep, double repBase, double sa, double gdp, double r, int captured)
    {
        var st = R788S.GetValue(d, _ => new R788State());
        int[] info = st.Info.TryGetValue(war, out var ii) ? ii : [-1, -1, -1, -1, -1];
        var ra = new int[3]; var rb = new int[3];
        R788Count(war.A, ra, out double pa, out double pca); R788Count(war.B, rb, out double pb, out double pcb);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        st.Rows.Add(string.Join('\t', I(st.Month), I(st.Year), I(info[0]), I(info[1]), I(info[2]), I(info[3]), I(info[4]), I(war.Months), war.CivilWarParent is not null ? "1" : "0",
            NameOf(war.A), NameOf(war.B), F(x.Score), x.ACol ? "1" : "0", x.BCol ? "1" : "0", x.Dec ? "1" : "0", x.Weary ? "1" : "0", x.Forced ? "1" : "0", I(x.FA), I(x.FB),
            F(x.PushA), F(x.PushB), F(x.ExA), F(x.ExB), hit ? "1" : "0", F(rep), F(repBase), F(sa), F(gdp), F(r), I(captured), F(x.TA), F(x.TB),
            I(x.RA[0]), I(x.RA[1]), I(x.RA[2]), I(ra[0]), I(ra[1]), I(ra[2]), I(x.RB[0]), I(x.RB[1]), I(x.RB[2]), I(rb[0]), I(rb[1]), I(rb[2]),
            F(x.PcA), F(pca), F(x.PcB), F(pcb)));
    }
    static void R788Table(Sink sk, Diplomacy d)
    {
        if (!R788S.TryGetValue(d, out var st)) return;
        using var wr = Open("r788", sk.Seed);
        wr.WriteLine("month\tyear\twarIdx\trootIdx\tdepth\tsuzFlag\tstartMonth\tmonths\tcivil\tA\tB\tscore\taCol\tbCol\tdecisive\tweary\tforced\tfrontsA\tfrontsB\tpushA\tpushB\texA\texB\thit\trep\trepBase\tsa\tloserGdpPerMonth\tloserR\tcaptured\ttreasA\ttreasB\taCore0\taInt0\taCol0\taCore1\taInt1\taCol1\tbCore0\tbInt0\tbCol0\tbCore1\tbInt1\tbCol1\taHomePop0\taHomePop1\tbHomePop0\tbHomePop1");
        foreach (var row in st.Rows) wr.WriteLine(row);
    }
}
'''
open(os.path.join(core, 'InstrumentsR788.cs'), 'w', encoding='utf-8', newline='').write(probe)
print('PATCHED')
