using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1291) c133b1：貿易の B-1＝EndowmentCostFollowsAbundance の既定 1（費用が賦存に従う）。事前登録 measurements/m1291_20261122_prereg_b1.txt の U1〜U3。
/// 盤面は試験の中で置く（同じ財・同じ限界費用の生産者に賦存倍率 2.0・1.0・0.4 を当てる）＋開幕の世界の播種の実経路。
/// ⚠旗 0（既定 0）で U1・U3 が赤・突然変異〈門を外す〉〈指数 2〉〈希少の側も割る〉〈播種の呼び口を飛ばす〉で赤（表は事前登録の [4]）。</summary>
public class EndowmentCostTests
{
    [Fact]
    public void U1_既定の係数で賦存倍率2の州の限界費用は倍率1の州の半分_旗0では割らない()
    {
        static (double rich, double plain) Board(Coefficients c)
        {
            var rich = new Producer { Productivity = 1.0, MarginalCost = 8.0 };
            var plain = new Producer { Productivity = 1.0, MarginalCost = 8.0 };
            Simulation.DivideCostByEndowment(rich, 2.0, c);
            Simulation.DivideCostByEndowment(plain, 1.0, c);
            return (rich.MarginalCost, plain.MarginalCost);
        }
        var (r1, p1) = Board(new Coefficients());
        Assert.Equal(8.0, p1);
        Assert.True(System.Math.Abs(p1 / r1 - 2.0) <= 1e-12, $"既定の係数で倍率 1.0÷倍率 2.0 の限界費用の比が 2.0 でない：{p1 / r1}");
        var (r0, p0) = Board(new Coefficients { EndowmentCostFollowsAbundance = 0.0 });
        Assert.Equal(8.0, p0);
        Assert.True(System.Math.Abs(p0 / r0 - 1.0) <= 1e-12, $"旗 0 の盤面で倍率 2.0 の州の限界費用が割られた：比 {p0 / r0}");
    }

    [Fact]
    public void U2_既定の係数で非産地の限界費用と能力は動かない()
    {
        var c = new Coefficients();
        var scarce = new Producer { Productivity = 1.0, MarginalCost = 8.0 };
        var rich = new Producer { Productivity = 1.0, MarginalCost = 8.0 };
        Simulation.DivideCostByEndowment(scarce, 0.4, c);
        Simulation.DivideCostByEndowment(rich, 2.0, c);
        Assert.Equal(8.0, scarce.MarginalCost);   // 倍率<1 は Scarcity の旗（既定 0）の持ち場＝上がらない
        Assert.Equal(1.0, scarce.Productivity);
        Assert.Equal(1.0, rich.Productivity);     // 費用だけを割る（能力は掛けない）
    }

    [Fact]
    public void U3_開幕の世界で賦存州の自己費用は非賦存州の自己費用を賦存倍率で割った値()
    {
        static Simulation Seed(Coefficients c) =>
            new(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1, Coef = c }, new NullAgent());
        static Producer? Rgo(Simulation s, string city, int good) =>
            s.World.Regions.Find(r => r.Name == city)?.Producers.Find(pr => pr.GoodId == good && pr.IsRgo);

        var cfg = WorldData.ByName("イギリス");
        var sim = Seed(new Coefficients());
        int checkedPairs = 0;
        foreach (var p in cfg.Provinces)
        foreach (int g in p.Resources)
        {
            var e = Rgo(sim, p.City, g);
            if (e is null || e.SiteRow >= 0) continue;
            foreach (var q in cfg.Provinces)
            {
                if (System.Array.IndexOf(q.Resources, g) >= 0) continue;
                var n = Rgo(sim, q.City, g);
                if (n is null || n.SiteRow >= 0) continue;
                double div = (Catalog.IsMineral(g) ? 4.0 : 3.5) * sim.World.Coef.ResourceEndowmentScale;   // 賦存の割る数＝鉱物 4.0／その他 3.5（world の注のデータ・エンジンの関数を呼ばない）
                double want = n.MarginalCost / div;
                Assert.True(System.Math.Abs(e.MarginalCost - want) <= 1e-9 * want,
                    $"{p.City}・{Catalog.Goods[g].Name}：賦存州の自己費用 {e.MarginalCost} ≠ 非賦存州 {q.City} の {n.MarginalCost}÷{div}");
                checkedPairs++;
                break;
            }
        }
        Assert.True(checkedPairs >= 3, $"賦存州と非賦存州の組が 3 未満（{checkedPairs}）＝試験にならない");
    }
}
