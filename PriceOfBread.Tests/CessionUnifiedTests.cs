using System.Linq;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(723) 線 ces2：旗 <see cref="Coefficients.CessionUnified"/>（講和の割譲の 4 経路を 1 本の判定へ・設計書 20261031_割譲の候補と費用_設計.md 段2・案 E2）の番人。
/// 盤面は ColonyCessionDistanceTests・CessionInstrumentTests と同じ（勝者＝フランス・敗者＝イギリス・第三者＝スペイン・凍結した外交・艦の強さを手で置く）。
/// 期待値は設計書 §4-3（(723) で置き直した手の値）から取る：パリからの大圏距離（マンチェスター 606 km・ダブリン 781 km・グラスゴー 898 km・バーミンガム 503 km＝港でない）と
/// world.json の `coastal`（バーミンガムだけが内陸）と、費用の下限（基本の費用 30）。本体の関数（CessionTarget・CessionCost）から期待値を作らない。
/// 旗 0 の番人（(iv)）は、旗 0 の盤面の結果が器を入れる前の木と同じ州（マドラスとバーミンガム）であること（旗の判定の突然変異で赤になる）。</summary>
[Collection("Instruments")]
public class CessionUnifiedTests
{
    private readonly ITestOutputHelper _out;
    public CessionUnifiedTests(ITestOutputHelper o) { _out = o; }

    private static MultiNationSim Board(double flag, double w, double l, double t)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.Coef.CessionUnified = flag;
        var fr = sim.Nations[0].World; var uk = sim.Nations[1].World; var es = sim.Nations[2].World;
        foreach (var (p, nav) in new[] { (fr.SelfPower!, w), (uk.SelfPower!, l), (es.SelfPower!, t) })
        { p._fleetDriven = false; p.Military = 1000; p.NavalShare = new Share(nav / 1000.0); p.NavySkillMul = 1.0; }
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is null) p.Military = 0;
        return sim;
    }

    /// <summary>勝者が受け取ったイギリスの州の名前（並べ替え済み）と、計器の行（経路の印）。</summary>
    private static (string[] moved, List<string[]> rows) Peace(MultiNationSim sim, double score)
    {
        var fr = sim.Nations[0].World; var uk = sim.Nations[1].World;
        var ukBefore = uk.Regions.Select(r => r.Name).ToHashSet();
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cessu_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = uk.SelfPower!, Score = score });
            var rows = Instruments.CessRowsPending(sim.Diplomacy).Select(r => r.Split('\t')).ToList();
            var moved = fr.Regions.Where(r => ukBefore.Contains(r.Name)).Select(r => r.Name).OrderBy(x => x, System.StringComparer.Ordinal).ToArray();
            return (moved, rows);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static string Col(string[] row, string name) { int i = System.Array.IndexOf(Instruments.CessHeaderText.Split('\t'), name); Assert.True(i >= 0, "列が無い " + name); return row[i]; }

    /// <summary>(i) の盤面：イギリスの植民地格の州を全て Integrated へ置き換える（植民地格の州を持たせない＝② は何も取らない・予算 100 は ③ に回る）。</summary>
    private static void NoColonialRegions(MultiNationSim sim)
    {
        foreach (var r in sim.Nations[1].World.Regions) if (r.Tier == RegionTier.Colonial) r.Tier = RegionTier.Integrated;
    }

    // ---------------------------------------------------------------- (iv) 旗 0 は今の 4 経路と同じ州を選ぶ ----------------------------------------------------------------

    [Fact]
    public void 旗0のマドラスの盤面は経路2がマドラス_経路3がバーミンガムを取る()
    {
        var sim = Board(0, 100, 10, 50);
        var (moved, rows) = Peace(sim, 1.3);   // 決定的勝利（1.2 以上 1.4 未満）＝② の後に ③ の境界改定
        foreach (var r in rows) _out.WriteLine(string.Join("|", r.Take(10)));
        var r2 = rows.Where(r => Col(r, "route") == "2").ToList();
        var r3 = rows.Where(r => Col(r, "route") == "3").ToList();
        Assert.Single(r2); Assert.Equal("マドラス", Col(r2[0], "region"));
        Assert.Single(r3); Assert.Equal("バーミンガム", Col(r3[0], "region"));   // 首都から最も近い非首都（503 km・内陸）＝今のとおり
        Assert.Equal(new[] { "バーミンガム", "マドラス" }, moved);
    }

    [Fact]
    public void 旗0の植民地格の州を持たない盤面は経路3がバーミンガムを取る()
    {
        var sim = Board(0, 100, 10, 50);
        NoColonialRegions(sim);
        var (moved, rows) = Peace(sim, 1.3);
        Assert.Equal(new[] { "バーミンガム" }, moved);
        Assert.DoesNotContain(rows, r => Col(r, "route") == "2");
    }

    // ---------------------------------------------------------------- (i) 内陸で隣り合わない州は候補にならない ----------------------------------------------------------------

    [Fact]
    public void 旗1は内陸のバーミンガムを飛ばして最も近い港のマンチェスターを取る()
    {
        var sim = Board(1, 100, 10, 50);
        NoColonialRegions(sim);
        var (moved, rows) = Peace(sim, 1.3);
        foreach (var r in rows) _out.WriteLine(string.Join("|", r.Take(14)));
        var r3 = rows.Where(r => Col(r, "route") == "3").ToList();
        Assert.Single(r3);
        Assert.Equal("マンチェスター", Col(r3[0], "region"));   // 港 3 州（マンチェスター 606・ダブリン 781・グラスゴー 898 km）のうち最も近い
        Assert.Equal(new[] { "マンチェスター" }, moved);
        // 費用は基本の費用 30＋10×資源 2 以上（人口の項は非負）で予算 100 以内・払った分だけ残りが減る
        double cost = double.Parse(Col(r3[0], "costEngineIndex"), System.Globalization.CultureInfo.InvariantCulture);
        double before = double.Parse(Col(r3[0], "budgetBeforeIndex"), System.Globalization.CultureInfo.InvariantCulture), after = double.Parse(Col(r3[0], "budgetAfterIndex"), System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(cost, 50.0, 100.0);
        Assert.Equal(100.0, before, 6);
        Assert.Equal(before - cost, after, 6);
    }

    [Fact]
    public void 艦10対100では港の州は取れず旗0は同じ陸塊のジブラルタル_旗1は陸で隣り合うフリータウンだけが移る()
    {
        // (ii)：艦 10 対 100（制海権 0.09＜0.65）＝港の州は上陸の門を通らない（マドラスもマンチェスターも移らない）。バーミンガムは内陸で隣り合わない。
        // 旗 0：ジブラルタルはパリと同じ陸塊（ユーラシア）＝旧の渡海の門が掛からず、4,000 km の円の内（約 1,500 km）＝③ で移る。
        // 旗 1：ジブラルタルは陸路グラフでフランスの州と隣り合わず（間にスペイン）港の上陸の門も閉じている＝移らない。代わりに、陸路グラフで辺（West Africa の海岸の隣）を共有するフランスのセネガルと
        //   隣り合うイギリスの植民地格のフリータウン（無資源・人口 15 千人＝費用は基本の 30 ほど）が ② で移る＝艦が無くても陸で隣り合う州は候補。
        var (movedOld, _) = Peace(Board(0, 10, 100, 50), 1.3);
        Assert.Equal(new[] { "ジブラルタル" }, movedOld);
        var (movedNew, rowsNew) = Peace(Board(1, 10, 100, 50), 1.3);
        Assert.Equal(new[] { "フリータウン" }, movedNew);
        Assert.Single(rowsNew);
        Assert.Equal("2", Col(rowsNew[0], "route")); Assert.Equal("1", Col(rowsNew[0], "landAdjFlag")); Assert.Equal("0", Col(rowsNew[0], "canCrossFlag"));
    }

    // ---------------------------------------------------------------- (iii) 予算は講和 1 回に 1 本 ----------------------------------------------------------------

    [Fact]
    public void 旗1は経路2が予算を使った後の残りで払えない経路3の州を取らない()
    {
        var sim = Board(1, 100, 10, 50);
        var (moved, rows) = Peace(sim, 1.3);
        foreach (var r in rows) _out.WriteLine(string.Join("|", r.Take(14)));
        var r2 = rows.Where(r => Col(r, "route") == "2").ToList();
        Assert.Single(r2); Assert.Equal("マドラス", Col(r2[0], "region"));
        double left = double.Parse(Col(r2[0], "budgetAfterIndex"), System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(left, 0.0, 30.0);                          // 残りは基本の費用 30 より小さい（設計書の手の値 27.42）
        Assert.DoesNotContain(rows, r => Col(r, "route") == "3");   // どの港の州も基本の費用 30 以上＝払えない
        Assert.Equal(new[] { "マドラス" }, moved);
    }

    // ---------------------------------------------------------------- 費用の距離の項：世界最大に及ばない勝者の遠く高い州は予算から外れる（旗 0 の hcd と同じ結果） ----------------------------------------------------------------

    [Fact]
    public void 旗1は艦が世界最大の三分の一の勝者の割譲でカナダだけが移る()
    {
        // ColonyCessionDistanceTests の手の算術と同じ盤面（勝者 100・第三者 300）：カナダ（基本 60・海路 6,600km 超）は距離込みで約 87、残りでは何も買えない。
        var a = Peace(Board(0, 100, 10, 300), 1.0).moved; var b = Peace(Board(1, 100, 10, 300), 1.0).moved;
        Assert.Equal(new[] { "カナダ" }, a);
        Assert.Equal(a, b);
    }

    // ---------------------------------------------------------------- (vi) 座標の無い州 ----------------------------------------------------------------

    [Fact]
    public void 座標の無い州は旗0でも旗1でも候補で距離の項が0()
    {
        foreach (double flag in new[] { 0.0, 1.0 })
        {
            var sim = Board(flag, 100, 10, 50);
            NoColonialRegions(sim);
            var uk = sim.Nations[1].World;
            var birm = uk.Regions.First(r => r.Name == "バーミンガム");
            birm.Name = "座標の無い小さな町";   // places.json に無い名前＝Climate.CityCoord が (0,0)
            var (moved, rows) = Peace(sim, 1.3);
            Assert.Equal(new[] { "座標の無い小さな町" }, moved);   // 距離 0＝最も近い（旧）・隣接を判定せず候補（旗 1）
        }
    }

    // ---------------------------------------------------------------- 旗 1 の ColonyList（抽象標的の植民地） ----------------------------------------------------------------

    [Fact]
    public void 旗1のColonyListは艦のある勝者には取れて艦の無い勝者には取れない()
    {
        // 抽象標的の植民地は琉球 1 件（前提）。艦 100 の勝者は旗 0 も旗 1 も取る。艦 0 の勝者は旗 0（4,000 km 超の門）も旗 1（陸で隣り合わず上陸の門も閉じている）も取らない。
        foreach (var (navy, want) in new[] { (100.0, true), (0.0, false) })
            foreach (double flag in new[] { 0.0, 1.0 })
            {
                var sim = Board(flag, navy, 10, 50);
                var fr = sim.Nations[0].World;
                var abs = sim.Diplomacy.Powers.FirstOrDefault(p => p.Nation is null && p.ColonyList.Count > 0);
                Assert.NotNull(abs);
                Assert.Equal(new[] { "琉球" }, abs!.ColonyList.Select(c => c.Name).ToArray());
                sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = abs, Score = 1.0 });
                Assert.Equal(want, abs.ColonyList.Count == 0);
            }
    }
}
