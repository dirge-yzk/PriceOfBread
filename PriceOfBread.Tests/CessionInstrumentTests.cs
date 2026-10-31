using System.Linq;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(717) 線 ces1：割譲の表 <c>cess_s</c>・講和の表 <c>cessp_s</c>（読むだけの計器・恒等）の番人。
/// 盤面は ColonyCessionDistanceTests と同じ（勝者＝フランス・敗者＝イギリス・第三者＝スペイン・凍結した外交・艦の強さを手で置く）。
/// 期待値は計器の式を写さず、地理（マドラスは港で、フランスの州と陸で隣り合わない）と講和の結果（実際に移った州の名前と数）から取る。
/// 陸の隣接の写し（<see cref="Instruments.CessAdjacent"/>）は <see cref="ArmyShape.Borders"/> と突き合わせる（辺の判定が替わったとき追いつかない写しの番人）。</summary>
[Collection("Instruments")]
public class CessionInstrumentTests
{
    private readonly ITestOutputHelper _out;
    public CessionInstrumentTests(ITestOutputHelper o) { _out = o; }

    private static MultiNationSim Board(double w, double l, double t)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var fr = sim.Nations[0].World; var uk = sim.Nations[1].World; var es = sim.Nations[2].World;
        foreach (var (p, nav) in new[] { (fr.SelfPower!, w), (uk.SelfPower!, l), (es.SelfPower!, t) })
        { p._fleetDriven = false; p.Military = 1000; p.NavalShare = new Share(nav / 1000.0); p.NavySkillMul = 1.0; }
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is null) p.Military = 0;
        return sim;
    }

    private static string[] Header => Instruments.CessHeaderText.Split('\t');
    private static string Col(string[] row, string name) { int i = System.Array.IndexOf(Header, name); Assert.True(i >= 0, "列が無い " + name); return row[i]; }

    private static List<string[]> Peace(MultiNationSim sim, double score, out List<string[]> peaces, out string dir)
    {
        dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cess_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var fr = sim.Nations[0].World; var uk = sim.Nations[1].World;
            sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = uk.SelfPower!, Score = score });
            peaces = Instruments.CessPeacesPending(sim.Diplomacy).Select(r => r.Split('\t')).ToList();
            return Instruments.CessRowsPending(sim.Diplomacy).Select(r => r.Split('\t')).ToList();
        }
        finally { Instruments.Configure(null, null); }
    }

    [Fact]
    public void 植民地格の州の複数割譲は経路2の行で書かれ地理と合う()
    {
        var sim = Board(100, 10, 50);   // 勝者の艦が世界最大
        var uk = sim.Nations[1].World; var fr = sim.Nations[0].World;
        var ukBefore = uk.Regions.Select(r => r.Name).ToHashSet();
        var rows = Peace(sim, 1.0, out var peaces, out var dir);   // 戦勝点 1.0＝割譲の線 1.2 未満＝植民地の争奪だけ
        try
        {
            var moved = fr.Regions.Where(r => ukBefore.Contains(r.Name)).Select(r => r.Name).OrderBy(x => x, System.StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { "マドラス" }, moved);   // 前提：ColonyCessionDistanceTests と同じ盤面で 1 件移る
            Assert.Equal(moved.Length, rows.Count);       // 行の数＝移った州の数
            var r0 = rows[0];
            Assert.Equal("マドラス", Col(r0, "region")); Assert.Equal("2", Col(r0, "route")); Assert.Equal("Colonial", Col(r0, "tier"));
            Assert.Equal("1", Col(r0, "winnerIsNationFlag")); Assert.Equal("1", Col(r0, "loserIsNationFlag"));
            Assert.Equal("1", Col(r0, "portFlag"));       // マドラスは港
            Assert.Equal("0", Col(r0, "landAdjFlag"));    // フランスのポンディシェリは陸路グラフでマドラスと辺で結ばれない（同じノードでもない）
            Assert.Equal("0", Col(r0, "sameNodeFlag"));
            Assert.Equal("1", Col(r0, "sameLandmassFlag")); // パリもマドラスもユーラシア（陸塊の判定はユーラシア 1 枚）
            double gc = double.Parse(Col(r0, "gcKm"), System.Globalization.CultureInfo.InvariantCulture), lk = double.Parse(Col(r0, "landKm"), System.Globalization.CultureInfo.InvariantCulture), sk = double.Parse(Col(r0, "seaKm"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(gc, 7000, 9000);               // パリ〜マドラスの大圏は約 8,000 km
            Assert.True(lk > gc && sk > gc, "陸路・海路は大圏より長い");
            Assert.Equal("1", Col(r0, "canCrossFlag"));   // 艦 100 対 10＝制海権 0.91
            double rel = double.Parse(Col(r0, "relSeaControlShare"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(rel, 0.90, 0.92);
            double ws = double.Parse(Col(r0, "worldNavShare"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(1.0, ws, 6);                     // 世界最大の艦
            double before = double.Parse(Col(r0, "budgetBeforeIndex"), System.Globalization.CultureInfo.InvariantCulture), after = double.Parse(Col(r0, "budgetAfterIndex"), System.Globalization.CultureInfo.InvariantCulture);
            double cost = double.Parse(Col(r0, "costEngineIndex"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(100.0, before, 6);               // 戦勝点 1.0×ColonyCaptureBudget（既定 100）
            Assert.Equal(before - cost, after, 6);        // 予算の前後の差＝払った費用
            Assert.Equal("1", Col(r0, "e2OkFlag"));       // 港で渡れる＝案 E2 の海の候補
            // 経路 2 は他の植民地格の州も候補の母集団に数える（マドラスを除く）
            int pool = int.Parse(Col(r0, "poolCount"));
            Assert.True(pool >= 3, $"植民地格の他の州の母集団が小さすぎる（{pool}）");
            // 講和の表：1 回・経路 2 が 1 件
            Assert.Single(peaces);
            Assert.Equal("1", peaces[0][System.Array.IndexOf(Instruments.CessPeaceHeaderText.Split('\t'), "n2Count")]);
            Assert.Equal("0", peaces[0][System.Array.IndexOf(Instruments.CessPeaceHeaderText.Split('\t'), "n3Count")]);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 決定的勝利の境界改定は経路3の行で書かれ移った州と一致する()
    {
        var sim = Board(100, 10, 50);
        var uk = sim.Nations[1].World; var fr = sim.Nations[0].World;
        var ukBefore = uk.Regions.Select(r => r.Name).ToHashSet();
        var rows = Peace(sim, 1.3, out var peaces, out var dir);   // 1.2 以上 1.4 未満＝決定的勝利（壊滅でない）＝境界改定
        try
        {
            var moved = fr.Regions.Where(r => ukBefore.Contains(r.Name)).Select(r => r.Name).ToHashSet();
            var r3 = rows.Where(r => Col(r, "route") == "3").ToList();
            _out.WriteLine($"移った州 {string.Join("・", moved)}／経路3の行 {r3.Count}／全行 {rows.Count}");
            foreach (var r in rows) _out.WriteLine(string.Join("\t", Header.Zip(r, (h, v) => h + "=" + v)));
            Assert.True(r3.Count <= 1, "境界改定は 1 講和に高々 1 件");
            Assert.Equal(moved.Count, rows.Count);   // 移った州ごとに 1 行（②が先に動いても同じ）
            foreach (var r in rows) Assert.Contains(Col(r, "region"), moved);
            // 案 E2 の候補の旗は、隣接・港・上陸の門の 3 列から取り直した値と全ての行で合う
            foreach (var r in rows)
            {
                bool want = Col(r, "landAdjFlag") == "1" || (Col(r, "portFlag") == "1" && Col(r, "canCrossFlag") == "1");
                Assert.Equal(want ? "1" : "0", Col(r, "e2OkFlag"));
            }
            if (r3.Count == 1)
            {
                var r = r3[0];
                Assert.Equal("nan", Col(r, "costEngineIndex"));   // エンジンは③に費用を持たない
                double reach = double.Parse(Col(r, "reachKm"), System.Globalization.CultureInfo.InvariantCulture);
                double gcKm = double.Parse(Col(r, "gcKm"), System.Globalization.CultureInfo.InvariantCulture), rel = double.Parse(Col(r, "relSeaControlShare"), System.Globalization.CultureInfo.InvariantCulture);
                // 海を渡る州（勝者の首都と別の陸塊）の実効距離＝大圏×(1＋CessionSeaCrossingWeight×(1−相対の制海権))。同じ陸塊なら大圏のまま。
                double want = Col(r, "sameLandmassFlag") == "1" ? gcKm : gcKm * (1.0 + sim.Coef.CessionSeaCrossingWeight * (1.0 - rel));
                Assert.Equal(want, reach, 4);
                Assert.Equal("0", Col(r, "colonialReachOnlyFlag"));   // 4,000 km の円の内
            }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 抽象標的の植民地の奪取は経路1の行で書かれる()
    {
        var sim = Board(100, 10, 50);
        var fr = sim.Nations[0].World;
        var abs = sim.Diplomacy.Powers.FirstOrDefault(p => p.Nation is null && p.ColonyList.Count > 0);
        Assert.NotNull(abs);   // 前提：植民地を持つ抽象標的が居る
        int n0 = abs!.ColonyList.Count;
        var names = abs.ColonyList.Select(c => c.Name).ToHashSet();
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cess_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = abs, Score = 1.0 });
            var rows = Instruments.CessRowsPending(sim.Diplomacy).Select(r => r.Split('	')).ToList();
            int taken = n0 - abs.ColonyList.Count;
            Assert.True(taken >= 1, "前提：講和で 1 件以上奪う");
            Assert.Equal(taken, rows.Count);
            Assert.All(rows, r => { Assert.Equal("1", Col(r, "route")); Assert.Equal("ColonyList", Col(r, "tier")); Assert.Equal("0", Col(r, "loserIsNationFlag")); Assert.Contains(Col(r, "region"), names); Assert.Equal("nan", Col(r, "portFlag")); });
            // 費用はエンジンが払った額＝予算の前後の差
            foreach (var r in rows)
                Assert.Equal(double.Parse(Col(r, "budgetBeforeIndex"), System.Globalization.CultureInfo.InvariantCulture) - double.Parse(Col(r, "costEngineIndex"), System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(Col(r, "budgetAfterIndex"), System.Globalization.CultureInfo.InvariantCulture), 6);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 計器は世界を変えない_有りと無しで割譲の結果が同じ()
    {
        string Run(bool on)
        {
            var sim = Board(100, 10, 50);
            var fr = sim.Nations[0].World; var uk = sim.Nations[1].World;
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cess_" + Guid.NewGuid().ToString("N"));
            if (on) Instruments.Configure(dir, "*");
            try { sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = uk.SelfPower!, Score = 1.3 }); }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
            return string.Join("|", sim.Nations.Select(n => n.World.Regions.Count + ":" + string.Join(",", n.World.Regions.Select(r => r.Name + "/" + r.SumPopulation().ToString("R")))));
        }
        Assert.Equal(Run(false), Run(true));
    }

    [Fact]
    public void 陸の隣接の写しはArmyShapeのBordersと国の組で一致する()
    {
        var names = new[] { "フランス", "プロイセン", "オーストリア", "スペイン", "ロシア", "オスマン", "イギリス", "日本" };
        var sim = new MultiNationSim(names.Select(OpeningFixture.Named).ToArray(), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var ps = sim.Nations.Select(n => n.World.SelfPower!).ToList();
        var b = ArmyShape.Borders(ps, sim.Coef);
        int n = ps.Count, agree = 0, adjPairs = 0, nonAdjPairs = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                // 州ごとの隣接の写しを国の組へ畳む：i の州のどれかが j の州のどれかと隣り合う
                var jNodes = new HashSet<int>();
                foreach (var r in ps[j].Nation!.Regions)
                {
                    var cc = Climate.CityCoord(r.Name);
                    if (cc.Lat == 0 && cc.Lon == 0) continue;
                    int nd = PathGraph.NearestNode(cc.Lat, cc.Lon);
                    if (nd >= 0) jNodes.Add(nd);
                }
                bool any = false;
                foreach (var r in ps[i].Nation!.Regions)
                {
                    var cc = Climate.CityCoord(r.Name);
                    if (cc.Lat == 0 && cc.Lon == 0) continue;
                    int nd = PathGraph.NearestNode(cc.Lat, cc.Lon);
                    if (nd >= 0 && Instruments.CessAdjacent(nd, jNodes, out _)) { any = true; break; }
                }
                bool border = b[i * n + j] > 0;
                if (any == border) agree++;
                if (border) adjPairs++; else nonAdjPairs++;
                _out.WriteLine($"{names[i]}→{names[j]}：写し {any}・Borders {b[i * n + j]:0.###}");
            }
        Assert.True(adjPairs > 0 && nonAdjPairs > 0, $"対照：陸で隣り合う組 {adjPairs}・隣り合わない組 {nonAdjPairs} の両方が要る");
        Assert.Equal(n * (n - 1), agree);
    }

    [Fact]
    public void 試合の終わりに見出しだけの表も書かれる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cess_" + Guid.NewGuid().ToString("N"));
        try
        {
            var sim = Board(100, 10, 50);
            Instruments.Configure(dir, "*");
            try
            {
                Instruments.Phase(sim, 1789UL, "open");
                var fr = sim.Nations[0].World; var uk = sim.Nations[1].World;
                sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = uk.SelfPower!, Score = 1.0 });
                Instruments.Close(sim);
            }
            finally { Instruments.Configure(null, null); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "cess_s1789.tsv"));
            Assert.Equal(Instruments.CessHeaderText, lines[0]);
            Assert.Equal(2, lines.Length);   // 見出し＋マドラス 1 件
            Assert.All(lines.Skip(1), l => Assert.Equal(Header.Length, l.Split('\t').Length));
            var pl = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "cessp_s1789.tsv"));
            Assert.Equal(Instruments.CessPeaceHeaderText, pl[0]); Assert.Equal(2, pl.Length);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
