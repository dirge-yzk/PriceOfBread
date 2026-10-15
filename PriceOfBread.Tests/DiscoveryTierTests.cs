using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(336) 流れG ② 系統×段階の番人（係数 <see cref="Coefficients.DiscoveryTierGate"/>・★(344) 既定 1＝点火）。
/// 形は AI の推奨〈引きなし・判定は各系統の次の段だけ〉に 作者 2026-10-02「すべて推奨でよい。」（(177)）。辺の表 8 本（腕 B）は AI の推奨に 作者 2026-10-15「特に違和感はないです。これでいきましょう。」（出典の頁は未確認）：前提の行がまだの国ではその行を判定しない（点数 0）・飛ばして先の段を解禁しない。
/// 期待値は表（<see cref="Catalog.DiscoveryAfter"/>）から作らない＝下の辺の一覧（史実の依存の順・事前登録 measurements/ln_20261015_prereg.txt [1] の表と同じ手書き）と、
/// 解禁の順序（k 回の判定で段 k までしか開かない）の関係で書く。</summary>
public class DiscoveryTierTests
{
    /// <summary>辺の一覧（子の行, 前提の行）＝★(344) 腕 B の 8 本（事前登録 [14-3]＝[1] の 20 本から 12 本を外した）。表の記入ミス（辺の取り違え・抜け）を止める手書きの写し。</summary>
    private static readonly (string Child, string Parent)[] Edges =
    [
        ("内燃機関", "蒸気機関"),
        ("装甲艦", "汽船"), ("弩級戦艦", "装甲艦"),
        ("戦車", "内燃機関"), ("軍用機", "内燃機関"), ("自動車", "内燃機関"),
        ("自転車", "加硫ゴム"),
        ("ラジオ", "電化製品"),
    ];

    private static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == name);

    /// <summary>手書きの辺から数えた段（根＝1）＝表の <see cref="Catalog.DiscoveryTier"/> と別の出所。</summary>
    private static int TierByEdges(string name)
    {
        int t = 1;
        while (System.Array.Find(Edges, e => e.Child == name) is { Child: not null } e) { name = e.Parent; t++; }
        return t;
    }

    private static MultiNationSim Board(ulong seed = 7, Coefficients? coef = null)
    {
        var sim = coef is null ? new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent())
                               : new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent(), coef);
        sim.StepQuarter();
        return sim;
    }

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    [Fact]
    public void 既定は点火で表の辺は手書きの一覧と一致し木で輪が無い()
    {
        Assert.Equal(1.0, new Coefficients().DiscoveryTierGate);
        var fromTable = Catalog.Discoveries.Where(d => d.After.Length > 0).Select(d => (d.Name, d.After)).OrderBy(x => x.Name, System.StringComparer.Ordinal).ToArray();
        var byHand = Edges.OrderBy(x => x.Child, System.StringComparer.Ordinal).ToArray();
        Assert.Equal(byHand.Select(x => (x.Child, x.Parent)).ToArray(), fromTable);
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
        {
            string n = Catalog.Discoveries[r].Name;
            Assert.Equal(TierByEdges(n), Catalog.DiscoveryTier(r));
            Assert.Equal(Catalog.DiscoveryAfter[r] < 0, Catalog.Discoveries[r].After.Length == 0);
            if (Catalog.DiscoveryAfter[r] >= 0) Assert.Equal(Catalog.Discoveries[Catalog.DiscoveryAfter[r]].Name, Catalog.Discoveries[r].After);
        }
        Assert.Equal(2, Catalog.DiscoveryTier(Row("ラジオ")));   // 電化製品→ラジオ
        Assert.Equal(3, Catalog.DiscoveryTier(Row("弩級戦艦")));   // 汽船→装甲艦→弩級戦艦
        Assert.Equal(3, Catalog.DiscoveryTier(Row("戦車")));       // 蒸気機関→内燃機関→戦車
        Assert.Equal(Row("蒸気機関"), Catalog.DiscoveryLine(Row("軍用機")));
        Assert.Equal(1, Catalog.DiscoveryTier(Row("機関車")));     // 機関車・汽船は根（(338) [15]＝伝わりの道を直してから辺を戻す）
        // 開幕の既知の発見（world.json）は前提を飛ばさない（既知の行の前提も既知）
        foreach (var n in WorldData.Nations())
            foreach (var name in n.KnownDiscoveries)
            {
                int r = Row(name);
                Assert.True(r >= 0, $"{n.Name}：{name} が表に無い");
                int a = Catalog.DiscoveryAfter[r];
                if (a >= 0) Assert.True(n.KnownDiscoveries.Contains(Catalog.Discoveries[a].Name), $"{n.Name}：開幕の既知 {name} の前提 {Catalog.Discoveries[a].Name} が既知でない");
            }
    }

    /// <summary>前提がまだの行は、接触が最大・技術が十分でも点数 0 で解禁せず（係数 1）、係数 0 なら同じ盤面で解禁する。前提の行がある国は前提の無い行と同じ点数。
    /// ★(344) 機関車の辺を外した（腕 B）ので、内燃機関 ← 蒸気機関で試す。</summary>
    [Fact]
    public void 係数が正なら前提がまだの行は判定せず係数0なら判定する()
    {
        var sim = Board();
        var w = Nation(sim, "清");
        int steam = Row("蒸気機関"), child = Row("内燃機関");
        w.Discovered[steam] = false; w.Discovered[child] = false;
        w.TechLevel = 20;   // 技術力は最大
        var contact = new double[Catalog.Discoveries.Length]; System.Array.Fill(contact, 1e6);   // 伝わりは最大（技術力70＋伝わり70≧100）
        var view = new Discovery.WorldView(contact);
        var off = Discovery.Evaluate(w, view, new Coefficients { DiscoveryTierGate = 0.0 });
        Assert.True(off[child].Score >= Discovery.GateScore, $"前提が崩れた（係数0で内燃機関が届かない {off[child]}）");
        var on = Discovery.Evaluate(w, view, new Coefficients { DiscoveryTierGate = 1.0 });
        Assert.Equal(0.0, on[child].Score);
        Assert.True(on[steam].Score >= Discovery.GateScore, "前提の無い行（蒸気機関）が判定されない");
        // 前提の無い行・前提が既知の行は係数に依らず同じ点数（ビット一致）
        for (int r = 0; r < off.Length; r++)
        {
            int a = Catalog.DiscoveryAfter[r];
            if (w.Discovered[r] || (a >= 0 && !w.Discovered[a])) continue;
            Assert.Equal(off[r], on[r]);
        }
        // Step：係数1（既定）では内燃機関は開かず、蒸気機関が開いた次の判定で開く／係数0では一度に開く
        var w1 = Nation(Board(), "清"); w1.Discovered[steam] = false; w1.Discovered[child] = false; w1.TechLevel = 20;
        var w0 = Nation(Board(), "清"); w0.Discovered[steam] = false; w0.Discovered[child] = false; w0.TechLevel = 20;
        Assert.Equal(1.0, w1.Coef.DiscoveryTierGate);
        var coef0 = w0.Coef; coef0.DiscoveryTierGate = 0.0;
        Discovery.Step(w0, view, coef0);
        Assert.True(w0.Discovered[child] && w0.Discovered[steam], "係数0：前提を読まず一度に開くはず");
        Discovery.Step(w1, view, w1.Coef);
        Assert.True(w1.Discovered[steam] && !w1.Discovered[child], "係数1：蒸気機関だけが開き内燃機関は同じ判定では開かない（連鎖で同時に開かない）");
        Discovery.Step(w1, view, w1.Coef);
        Assert.True(w1.Discovered[child], "係数1：蒸気機関が開いた次の判定で内燃機関が開く");
    }

    /// <summary>接触・技術を最大にして判定を k 回通すと、段 k までの行だけが開く（段の順に1段ずつ）＝段を飛ばした解禁が無い。期待の段は手書きの辺から数える。</summary>
    [Fact]
    public void 判定をk回通すと段k以下の行だけが開き段を飛ばさない()
    {
        var sim = Board();
        var w = Nation(sim, "清");
        for (int r = 0; r < w.Discovered.Length; r++) w.Discovered[r] = false;
        w.TechLevel = 50;
        var contact = new double[Catalog.Discoveries.Length]; System.Array.Fill(contact, 1e6);
        var view = new Discovery.WorldView(contact);
        var coef = w.Coef; coef.DiscoveryTierGate = 1.0;
        var judged = new System.Collections.Generic.List<int>();
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
            if (!Discovery.IsSpread(Catalog.Discoveries[r].Field)) judged.Add(r);   // 制度と思想は伝わりが到達＝この盤面では開かない
        for (int k = 1; k <= 5; k++)
        {
            var pre = Discovery.Evaluate(w, view, coef);   // ★(344) 判定の時点の点数（解禁済みの行は 0）
            Discovery.Step(w, view, coef);
            foreach (int r in judged)
            {
                string n = Catalog.Discoveries[r].Name;
                if (w.Discovered[r])
                {
                    Assert.True(TierByEdges(n) <= k, $"{n}：段 {TierByEdges(n)} が {k} 回目の判定で開いた");
                    var p = System.Array.Find(Edges, e => e.Child == n);
                    if (p.Child is not null) Assert.True(w.Discovered[Row(p.Parent)], $"{n}：前提 {p.Parent} が未解禁のまま解禁した（段を飛ばした）");
                }
            }
            // 清はこの盤面で代表の生産者を持つ行なら、段 k 以下は全て開く（接触と技術が最大＝点数 140 以上）
            // ★(344) 判定の後の点数でなく判定の時点の点数で読む＝辺を外した根の行（電動機など）は同じ判定で開いた投入（石炭火力）の後に点数が上がる（段とは別の依存）
            foreach (int r in judged)
            {
                if (TierByEdges(Catalog.Discoveries[r].Name) > k) continue;
                var sc = pre[r];
                if (sc.Score >= Discovery.GateScore) Assert.True(w.Discovered[r], $"{Catalog.Discoveries[r].Name}：段 {TierByEdges(Catalog.Discoveries[r].Name)} は {k} 回目までに開くはず");
            }
        }
        Assert.True(w.Discovered[Row("ラジオ")] && w.Discovered[Row("弩級戦艦")], "5 回の判定で段 2 のラジオと最も深い段 3 の弩級戦艦まで開くはず");
    }
}
