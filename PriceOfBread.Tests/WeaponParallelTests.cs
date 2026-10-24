using System;
using System.IO;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(537) 退場の廃止と武器の強さ（採った＝旗と退場の表は消した・事前登録 measurements/wp_20261024_prereg.txt の [9]）の番人。機構と不変条件だけを見る
/// （旧が退場しない・新の門・強さの束・印のセーブの往復）。新しいレシピが徐々に置き換える創発は成績表と計器 wp_s が見る。
/// ・期待値は本体の定数から作らない：強さの束は強さ k=1（束を変えない）の世界の生産者と並べて、渡した比 k で比べる。
/// ⚠突然変異（それぞれ赤になることを確かめる）：①旧に退場を戻す（Discovery.Gate に後継の発見で 0 を返す枝を足す） ②OpenedByRow を立てない ③強さ k を掛けない（ToWeaponStrength の if (k &gt; 0) を外す）。</summary>
public class WeaponParallelTests
{
    // 構築時に読む係数なので Simulation に係数つきの Scenario を渡す（MultiNationSim の係数は国の構築には届かない＝CriminalVesselTests と同じ注意）
    private static Simulation Make(double rifleK = 3.0, double artK = 4.0)
    {
        var c = new Coefficients { WeaponStrengthRifle = rifleK, WeaponStrengthArtillery = artK };
        return new Simulation(new Scenario { Kind = ScenarioKind.Nation, NationConfig = NationConfig.Britain(), Seed = 29, Coef = c }, new NullAgent());
    }

    private static (Producer Musket, Producer SteelRifle, Producer CastGun, Producer SteelGun, Producer CastEngine, Producer Bessemer, int Row) Pick(Region r)
    {
        var musket = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var rifle = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName != "マスケット");
        var castGun = Assert.Single(r.Producers, p => p.GoodId == Catalog.ArtilleryId && p.RecipeName == "鋳鉄砲");
        var gun = Assert.Single(r.Producers, p => p.GoodId == Catalog.ArtilleryId && p.RecipeName != "鋳鉄砲");
        var engine = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteamEngineId && p.RecipeName == "鋳鉄の機関");
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.DiscoveryRow >= 0);
        return (musket, rifle, castGun, gun, engine, bessemer, bessemer.DiscoveryRow);
    }

    private static double Mc(Producer p, Region r, Coefficients c, World w)
        => EconomyStep.MineralSelfCost(p, r, c, w) + EconomyStep.InputCostPerUnit(p, r, r, c);   // エンジンと同じ限界費用の式

    [Fact]
    public void 旧は転炉鋼を知っても退場せず産み続ける()
    {
        var sim = Make();
        var w = sim.World; var r = w.Region;
        var (musket, _, castGun, _, engine, _, row) = Pick(r);
        var old = new[] { musket, castGun, engine };
        r.Discovered = w.Discovered;
        Assert.False(Discovery.Known(w.Discovered, row), "前提が崩れている（転炉鋼が発見済み）");
        foreach (var p in old) Assert.Equal(1.0, Discovery.Gate(r, p));
        w.Discovered[row] = true;   // 後継の発見
        foreach (var p in old) Assert.Equal(1.0, Discovery.Gate(r, p));   // 退場しない
        sim.Run(2);
        foreach (var p in old)
        {
            Assert.Equal(1.0, Discovery.Gate(r, p));
            Assert.True(p.Scale > 0, $"{p.RecipeName}：転炉鋼の後に操業規模 {p.Scale}＝退場している（退場の表では 0 に置かれる）");
            Assert.NotEqual((byte)1, EconomyStep.SleepState(sim.World.Region, p, sim.World.Coef));   // 眠らない
        }
    }

    [Fact]
    public void 新の門は転炉鋼まで閉じ財の解禁は旧が保つ()
    {
        var sim = Make();
        var w = sim.World; var r = w.Region;
        var (_, rifle, _, gun, _, _, row) = Pick(r);
        r.Discovered = w.Discovered;
        Assert.Equal(row, rifle.OpenedByRow); Assert.Equal(row, gun.OpenedByRow);
        Assert.Equal(0.0, Discovery.Gate(r, rifle)); Assert.Equal(0.0, Discovery.Gate(r, gun));
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.RifleId));        // 小銃を作れる（旧がある）
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.ArtilleryId));
        w.Discovered[row] = true;
        Assert.Equal(1.0, Discovery.Gate(r, rifle)); Assert.Equal(1.0, Discovery.Gate(r, gun));
    }

    [Fact]
    public void 新の束は強さの比だけ投入が小さく生産性が大きい()
    {
        var off = Make(1.0, 1.0); var on = Make(3.0, 4.0);   // k=1＝束を変えない世界と並べる
        var (_, rifle0, _, gun0, _, _, _) = Pick(off.World.Region);
        var (musket1, rifle1, castGun1, gun1, _, _, _) = Pick(on.World.Region);
        void Check(Producer p0, Producer p1, double k)
        {
            Assert.Equal(p0.Productivity * k, p1.Productivity);
            Assert.Equal(p0.Inputs.Count, p1.Inputs.Count);
            for (int i = 0; i < p0.Inputs.Count; i++)
            {
                Assert.Equal(p0.Inputs[i].GoodId, p1.Inputs[i].GoodId);
                Assert.Equal(p0.Inputs[i].QtyPerOutput / k, p1.Inputs[i].QtyPerOutput);   // 労働と高度労働を含む全ての投入が 1/k
            }
            Assert.Equal(p0.MarginalCost, p1.MarginalCost);   // 自己費用は割らない
        }
        Check(rifle0, rifle1, 3.0); Check(gun0, gun1, 4.0);
        // 旧は強さを掛ける前の既定の生産性から作る
        Assert.Equal(rifle0.Productivity * on.World.Coef.MusketProductivity, musket1.Productivity);
        Assert.Equal(gun0.Productivity * on.World.Coef.MusketProductivity, castGun1.Productivity);
    }

    [Fact]
    public void 参照価格の世界で新は旧より1単位の費用が安く門が無ければ坩堝鋼の値段でも勝つ()
    {
        var sim = Make();
        var w = sim.World; var r = w.Region; var c = w.Coef;
        var (musket, rifle, castGun, gun, _, _, _) = Pick(r);
        for (int g = 0; g < Catalog.RefPrices.Length && g < r.Markets.Length; g++) r.Markets[g].Price = Catalog.RefPrices[g] * r.PriceLevel;
        Assert.True(Mc(rifle, r, c, w) < Mc(musket, r, c, w), "ref 価格で新の小銃が旧より高い");
        Assert.True(Mc(gun, r, c, w) < Mc(castGun, r, c, w), "ref 価格で新の砲が旧より高い（強さの比 k=1 では 4.10 対 2.50 で旧が安い＝強さの比が効いていない）");
        // 門が必要な理由：鋼が坩堝鋼の値段（ベッセマー以前）でも新が安い＝門が無ければ 1783 年から新が旧に勝つ
        var crucible = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.RecipeName == "坩堝鋼");
        r.Markets[Catalog.SteelId].Price = Mc(crucible, r, c, w);
        Assert.True(Mc(rifle, r, c, w) < Mc(musket, r, c, w), "坩堝鋼の値段でも新の小銃が旧より安い（門が要る理由）");
        Assert.True(Mc(gun, r, c, w) < Mc(castGun, r, c, w), "坩堝鋼の値段でも新の砲が旧より安い（門が要る理由）");
    }

    [Fact]
    public void 印はセーブの往復で保たれる()
    {
        // 印は構築時にだけ置かれるので、既定の世界の英の生産者へ手で置いて往復を見る（置く側は上の試験が固定する）
        var sim = new MultiNationSim([NationConfig.Britain()], seed: 29);
        var r0 = sim.Nations[0].World.Region;
        int row = Catalog.OpenedByRowOf(Catalog.RifleId);
        Assert.True(row >= 0);
        var musket0 = Assert.Single(r0.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var rifle0 = Assert.Single(r0.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName != "マスケット");
        rifle0.OpenedByRow = row;
        using var ms1 = new MemoryStream();
        SaveGame.Save(sim, ms1);
        ms1.Position = 0;
        var sim2 = SaveGame.Load(ms1);
        using var ms2 = new MemoryStream();
        SaveGame.Save(sim2, ms2);
        Assert.Equal(ms1.ToArray(), ms2.ToArray());
        var r = sim2.Nations[0].World.Region;
        var musket = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var rifle = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName != "マスケット");
        Assert.Equal(row, rifle.OpenedByRow); Assert.Equal(-1, musket.OpenedByRow);
    }

    [Fact]
    public void 計器wp_sの列は固定()
    {
        Assert.Equal("quarter\tyear\tnation\tgood\tknown\toldGateOpen\toldOutputPerMonth\tnewOutputPerMonth\tnewShare\toldCostPerUnit\tnewCostPerUnit\tgoodPriceMean\tpriceLevel\tcapitalEquipFill\tcapitalReadiness"
            + "\toldBuilt\tnewBuilt\toldStateTarget\tnewStateTarget\toldScale\tnewScale\tregionsBoth\tregionsOldCheaper\toldOutWhereOldCheaper\tnewOutWhereOldCheaper\toldOutWhereNewCheaper\tnewOutWhereNewCheaper",
            Instruments.WpHeaderText());
    }
}
