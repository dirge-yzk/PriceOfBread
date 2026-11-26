using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1371) c145rv 旗 WarWearyPoliticsCut：厭戦の政治の側の 3 本の読み（保持力の戦時の乗数・闘争度の目標の厭戦の項・天命の厭戦の項）を外す。
/// 盤面は試験の中で置く（仏と西の交戦中の盤面・厭戦だけを 0 と 0.8 に置く 2 つの同一の盤面）。前提の Assert で交戦中と厭戦の値を確かめる。
/// 旗 1＝3 つの量が厭戦に依らない・旗 0＝3 つとも厭戦で動く（盤面が厭戦に感度を持つことの対照）。
/// 突然変異 M1 保持力の枝を戻す→T3 赤・M2 闘争度の項を戻す→T1 赤・M3 天命の項を戻す→T2 赤（事前登録 m1371 の表）。</summary>
[Collection("Instruments")]
public class WarWearyPoliticsCutTests
{
    private static World Board(double flag, double exhaustion) => BoardSim(flag, exhaustion).w;

    private static (MultiNationSim sim, World w) BoardSim(double flag, double exhaustion)
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("スペイン")], seed: 1791UL) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var es = sim.Nations[1].World;
        w.Coef.RegionalRevoltThreshold = 0;
        w.Coef.WarWearyPoliticsCut = flag;
        w.PoliticsEnabled = true;
        w.BaseCoercion = 0.5; w.Legitimacy = 0.6; w.FiscalFundingRatio = new Share(0.7);
        w.SelfPower!.WarExhaustion = exhaustion;
        sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = es.SelfPower! });
        foreach (var p in w.AllPops()) p.Militancy = 0.1;
        Assert.True(Government.AtWar(w));                       // 前提＝交戦中
        Assert.Equal(exhaustion, w.SelfPower!.WarExhaustion);   // 前提＝厭戦を置いた値
        Assert.True(w.Coef.WarWearinessMilitancy > 0 && w.Coef.DynMandateWarWeight > 0 && w.Coef.WarCoercionDrain > 0);
        return (sim, w);
    }

    private static (double mil, double milWar) MilAfterStep(World w)
    {
        Politics.Step(w, w.Coef);
        double mil = 0, milWar = 0; int n = 0;
        foreach (var p in w.AllPops()) { if (p.Size <= 1e-9) continue; mil += p.Militancy * p.Size; milWar += p.MilWar; n++; }
        Assert.True(n > 20, $"POP が少ない {n}");
        return (mil, milWar);
    }

    [Fact]
    public void T1_旗1で闘争度の目標が厭戦に依らない()
    {
        var (a, aw) = MilAfterStep(Board(1, 0.0));
        var (b, bw) = MilAfterStep(Board(1, 0.8));
        Assert.Equal(0.0, bw);
        Assert.Equal(a, b, 12);
    }

    [Fact]
    public void T2_旗1で天命の目標が厭戦に依らない()
    {
        var w0 = Board(1, 0.0); var w8 = Board(1, 0.8);
        Politics.Step(w0, w0.Coef); Politics.Step(w8, w8.Coef);
        Assert.Equal(w0.DynasticMandate, w8.DynasticMandate, 12);
    }

    [Fact]
    public void T3_旗1で保持力の戦時の乗数が1()
    {
        var w0 = Board(1, 0.0); var w8 = Board(1, 0.8);
        double basis = w0.BaseCoercion * w0.Legitimacy * w0.FiscalFundingRatio.Value;   // 手で数えた式＝乗数 1
        Assert.Equal(basis, Politics.CoercionNow(w0, w0.Coef), 12);
        Assert.Equal(basis, Politics.CoercionNow(w8, w8.Coef), 12);
    }

    /// <summary>(1377) c146wy T5：計器 scalars_s の dmWar と milWarMean の列が旗を通る（盤面は T1〜T4 と同じ・厭戦 0.8・交戦中）。
    /// 期待値は試験の中で係数から書く（旗 1＝0・旗 0＝−DynMandateWarWeight×厭戦 と WarWearinessMilitancy×厭戦）。突然変異 M4（計器の :38 を旧の式へ）→旗 1 で赤。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T5_計器のdmWarとmilWarMeanは旗を通る(double flag)
    {
        const double exh = 0.8;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_wy_" + System.Guid.NewGuid().ToString("N"));
        var (sim, w) = BoardSim(flag, exh);
        Instruments.Configure(dir, "*");
        try
        {
            Politics.Step(w, w.Coef);
            Assert.Equal(exh, w.SelfPower!.WarExhaustion);   // 前提＝Politics.Step は厭戦を書かない（計器が読む厭戦は置いた値）
            Instruments.Quarter(sim, 1791UL, 3);              // q＝3＝年末＝全ての国の行
            Instruments.Close(sim);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "scalars_s1791.tsv"));
            var h = lines[0].Split('	');
            int iN = System.Array.IndexOf(h, "nation"), iD = System.Array.IndexOf(h, "dmWar"), iM = System.Array.IndexOf(h, "milWarMean"), iA = System.Array.IndexOf(h, "atWar");
            Assert.True(iN >= 0 && iD >= 0 && iM >= 0 && iA >= 0, "列が無い");
            var rows = lines.Skip(1).Select(l => l.Split('	')).Where(r => r[iN] == Instruments.NameOf(w)).ToList();
            Assert.Single(rows);
            var r = rows[0];
            Assert.Equal("1", r[iA]);                          // 前提＝交戦中の行
            double dm = double.Parse(r[iD], System.Globalization.CultureInfo.InvariantCulture);
            double mw = double.Parse(r[iM], System.Globalization.CultureInfo.InvariantCulture);
            if (flag > 0) { Assert.Equal(0.0, dm); Assert.Equal(0.0, mw); }
            else
            {
                Assert.Equal(-w.Coef.DynMandateWarWeight * exh, dm, 9);
                Assert.Equal(w.Coef.WarWearinessMilitancy * exh, mw, 9);   // 全ての人の MilWar が同じ値＝人口の加重平均も同じ
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void T4_旗0では3つとも厭戦で動く_対照()
    {
        var w0 = Board(0, 0.0); var w8 = Board(0, 0.8);
        Assert.NotEqual(Politics.CoercionNow(w0, w0.Coef), Politics.CoercionNow(w8, w8.Coef), 6);
        var (a, _) = MilAfterStep(w0); var (b, bw) = MilAfterStep(w8);
        Assert.True(bw > 0.2);                                   // 0.35×0.8＝0.28 が目標に入る
        Assert.NotEqual(a, b, 6);
        Assert.NotEqual(w0.DynasticMandate, w8.DynasticMandate, 6);
    }
}
