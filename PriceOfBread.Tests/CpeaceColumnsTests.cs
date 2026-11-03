using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(805) 線 c12cm：計器 <c>cpeace_s</c> の末尾 3 列（<c>repExpectIndex</c>・<c>repMovedOverExpect</c>・<c>repFactors</c>）の番人。
/// 盤面＝<see cref="CollapsePeaceTests"/> と同じ（敗者イギリスが 2 本の戦争で同じ月に厭戦の崩壊で負ける・旗 1）。ただし条約港の倍率を立てる
/// （<c>TreatyPortWinnerDistanceKm</c>＝100 km・パリもマドリードもロンドンから 100 km より遠い・戦勝点は崩壊の満額 1.5 で <c>VassalizeWinScore</c> 1.4 以上）。
/// 本国の割譲は <c>TerritoryCessionScore</c> を高く置いて外す（条約港の属国の枝に入らない）。
/// 期待値は手書きのリテラルの式：ReparationGdpFraction 0.2×敗者の GdpPerMonth×戦勝点 1（崩壊の満額の 1 回分）×条約港の倍率 2.5×支払い率 0.8（計器の式も本体の式も呼ばない）。
/// ⚠突然変異〈期待の側が条約港の倍率を掛けない〉〈エンジンが条約港の倍率を掛けない〉〈期待の側が r を掛けない〉〈repFactors に r を書かない〉で赤（measurements/m805_20261103_mutation.txt）。</summary>
[Collection("Instruments")]
public class CpeaceColumnsTests
{
    private static string[] Header => Instruments.CpeaceHeaderText.Split('\t');
    private static string Col(string[] row, string name) { int i = System.Array.IndexOf(Header, name); Assert.True(i >= 0, "列が無い " + name); return row[i]; }
    private static double Num(string[] row, string name) => double.Parse(Col(row, name), System.Globalization.CultureInfo.InvariantCulture);

    private static (string[] row, double gdp, double r) Run()
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.CollapsePeaceOneShare = 1;
        c.WarLuck = 0; c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;
        c.TreatyPortWinnerDistanceKm = 100;        // 条約港の倍率（遠い勝者）を立てる
        c.ConcertReparationRestraint = 0;          // 会議の削りは外す（repCut＝0）
        c.ColonyCaptureBudget = 0;
        c.TerritoryCessionScore = 1e9;             // 本国の割譲は外す
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!; var es = sim.Nations[2].World.SelfPower!;
        foreach (var p in new[] { fr, uk, es }) p.WarExhaustion = 0;
        uk.WarExhaustion = 1.0;
        uk.Nation!.FiscalFundingRatio = new Share(0.8);
        sim.Diplomacy.Wars.Add(new War { A = fr, B = uk, PushA = 3e12, PushB = 1 });
        sim.Diplomacy.Wars.Add(new War { A = uk, B = es, PushA = 1, PushB = 1e12 });
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cpeace_" + System.Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            Assert.Equal(2.5, c.TreatyPortReparationMult);    // 前提：条約港の倍率は 2.5
            sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);   // 四半期の AI・調停が走らない月
            double gdp = uk.GdpPerMonth.Value, r = uk.Nation!.FiscalFundingRatio.Value;   // 歩の中で同期された後の値（CollapsePeaceTests.Run と同じ）
            Assert.Empty(sim.Diplomacy.Wars);                    // 前提：2 本とも崩壊の講和で終わった
            var rows = Instruments.CpRowsPending(sim.Diplomacy);
            Assert.Single(rows);                                 // 前提：月×敗者の事象は 1 行（崩壊 2 本）
            Assert.True(gdp > 0, "前提が崩れた：敗者の GdpPerMonth が 0");
            return (rows[0].Split('\t'), gdp, r);
        }
        finally { Instruments.Configure(null, null); }
    }

    [Fact]
    public void 条約港の倍率と支払い率を掛けた期待の側が実際に移った賠償に一致する()
    {
        var (row, gdp, r) = Run();
        Assert.Equal(Header.Length, row.Length);
        Assert.Equal("2", Col(row, "nCollapse"));
        double want = 0.2 * gdp * 1.0 * 2.5 * r;   // 戦勝点 1（崩壊の満額を [-1,1] に切った 1 回分）・条約港の倍率 2.5・支払い率 r
        Assert.True(System.Math.Abs(want - Num(row, "repMovedIndex")) <= 1e-9 * want, $"実際に移った賠償 期待 {want:G17}・実 {Col(row, "repMovedIndex")}");
        Assert.True(System.Math.Abs(want - Num(row, "repExpectIndex")) <= 1e-9 * want, $"期待の側 期待 {want:G17}・実 {Col(row, "repExpectIndex")}");
        double ratio = Num(row, "repMovedOverExpect");
        Assert.True(System.Math.Abs(ratio - 1.0) <= 1e-9, $"実際÷期待 {ratio:G17}");
    }

    [Fact]
    public void 戦争ごとの因子の列に会議の削りと条約港の倍率と支払い率が書かれる()
    {
        var (row, _, r) = Run();
        var facs = Col(row, "repFactors").Split('/');
        Assert.Equal(2, facs.Length);   // 崩壊の講和 2 本
        foreach (var f in facs)
        {
            var p = f.Split(':').Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            Assert.Equal(3, p.Length);
            Assert.Equal(0.0, p[0]);      // 会議の削り repCut＝0
            Assert.Equal(2.5, p[1]);      // 条約港の倍率
            Assert.Equal(r, p[2], 12);    // 支払い率 r（0.8）
        }
    }
}
