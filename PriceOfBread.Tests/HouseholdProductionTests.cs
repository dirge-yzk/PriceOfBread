using System.Collections.Generic;
using System.IO;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(418) 日用品の自給（00-CE2 案 B）の番人（事前登録 measurements/ce_20261019_prereg.txt の D1〜D8）。
/// 旗 <c>HouseholdProductionEnabled</c>＝農家の手仕事を実の生産者の行（<c>RecipeName</c>＝〈家内工業〉・8 財）にする。期待値は本体の表（<c>Simulation.HouseholdRecipes</c>）から作らず、
/// 事前登録の D3 の手書きの表（財→原料と量）と、職人の工房の手計算の値（衣料の生産性 1.8×7×0.6・自己費用 0.35）で書く（同語反復にしない）。
/// 機構と不変条件の番人であって、創発の主張（手仕事の産出の割合・日用品の充足）は成績表と計器 <c>house_s</c> が見る。
/// ⚠州の構築時の旗：MultiNationSim の <c>coef</c> は各国の Simulation には届かない（各国は <c>cfg.ToScenario</c> の既定の係数＝POB_COEF だけが届く）＝本土の州は <c>Scenario.Coef</c> を直に差して建てる（下の <c>Nation</c>）。
/// 昇格した植民地の州は MultiNationSim の <c>coef</c> で建つ（下の <c>Multi</c>）。
/// ⚠突然変異（それぞれ赤になることを確かめた）：投入 1 本の入れ替え（衣料の染料→塩）／担い手を農民から貴族へ／旗の読み落とし（先頭の return を外す）／FacilityBuildScore の述語を外す／職人の比 0.6 を 0.5 に。</summary>
public class HouseholdProductionTests
{
    /// <summary>本土の州だけの 1 国の世界（旗は Scenario.Coef に差す＝州の構築時に届く）。</summary>
    private static World Nation(NationConfig cfg, double flag)
    {
        var sc = cfg.ToScenario(41, sharedWorld: true);
        typeof(Scenario).GetProperty(nameof(Scenario.Coef))!.SetValue(sc, new Coefficients { HouseholdProductionEnabled = flag });
        return new Simulation(sc, new NullAgent()).World;
    }

    private static IEnumerable<World> Worlds(double flag)
    {
        yield return Nation(NationConfig.Britain(), flag);
        yield return Nation(NationConfig.France(), flag);
    }

    private static MultiNationSim Multi(double flag)
        => new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83, coef: new Coefficients { HouseholdProductionEnabled = flag }) { DiplomacyFrozen = true };

    /// <summary>事前登録 D3 の手書きの表（産出 1 単位あたり）。</summary>
    private static readonly (int Good, (int In, double Q)[] Inputs)[] Expected =
    [
        (Catalog.ClothingId,      [(Catalog.CottonId, 0.6), (Catalog.DyeId, 0.2)]),
        (Catalog.FurnitureId,     [(Catalog.TimberId, 0.645), (Catalog.LivestockId, 0.15)]),
        (Catalog.PreservedMeatId, [(Catalog.LivestockId, 0.36), (Catalog.WheatId, 0.3)]),
        (Catalog.DriedFishId,     [(Catalog.FishId, 0.7), (Catalog.SaltId, 0.2)]),
        (Catalog.BeerId,          [(Catalog.WheatId, 0.5)]),
        (Catalog.SpiritsId,       [(Catalog.WheatId, 0.6)]),
        (Catalog.CheeseId,        [(Catalog.LivestockId, 0.6)]),
        (Catalog.LeatherGoodsId,  [(Catalog.LivestockId, 0.25), (Catalog.TimberId, 0.075), (Catalog.FurId, 0.2), (Catalog.DyeId, 0.2)]),
    ];

    private static void AssertHomeRows(Region r)
    {
        var rows = r.Producers.Where(p => p.RecipeName == "家内工業").ToList();
        Assert.Equal(8, rows.Count);
        for (int k = 0; k < Expected.Length; k++)
        {
            var row = rows[k];
            Assert.Equal(Expected[k].Good, row.GoodId);
            Assert.Equal(Expected[k].Inputs.Length, row.Inputs.Count);
            for (int j = 0; j < row.Inputs.Count; j++)
            {
                Assert.Equal(Expected[k].Inputs[j].In, row.Inputs[j].GoodId);
                Assert.Equal(Expected[k].Inputs[j].Q, row.Inputs[j].QtyPerOutput);
            }
            // 労働力・高度労働力・農業労働力は雇わない（自営）
            Assert.DoesNotContain(row.Inputs, i => i.GoodId == Catalog.LaborId || i.GoodId == Catalog.AdvLaborId || i.GoodId == Catalog.FarmLaborId);
            // 担い手＝所有者＝農民
            Assert.Equal(row.LaborPopIndex, row.OwnerPopIndex);
            Assert.Equal(PopClass.Peasant, r.Pops[row.LaborPopIndex].ClassId);
            Assert.False(row.BuysLabor); Assert.False(row.IsRgo);
            Assert.Equal(1.0, row.Scale); Assert.Equal(30.0, row.Output); Assert.Equal(0.35, row.MarginalCost); Assert.Equal(0.0, row.RentShare.Value);
        }
        Assert.Equal(1.8 * 7.0 * 0.6, rows[0].Productivity, 12);   // 衣料の生産性＝手計算 1.8×7×0.6
    }

    [Fact]
    public void 既定は休眠で職人の比と自己費用は手書きの値のまま()
    {
        Assert.Equal(0.0, new Coefficients().HouseholdProductionEnabled);
        Assert.Equal(0.6, Simulation.HandcraftProductivityRatio);
        Assert.Equal(0.35, Simulation.HandcraftSelfCost);
        Assert.Equal("家内工業", Simulation.HouseholdRecipeName);
    }

    [Fact]
    public void 旗0では手仕事の行は無く旗1は行を足すだけで他の行の並びを変えない()
    {
        var off = Worlds(0.0).ToList(); var on = Worlds(1.0).ToList();
        int regions = 0;
        for (int n = 0; n < off.Count; n++)
        {
            Assert.Equal(off[n].Regions.Count, on[n].Regions.Count);
            for (int i = 0; i < off[n].Regions.Count; i++)
            {
                regions++;
                var a = off[n].Regions[i]; var b = on[n].Regions[i];
                Assert.DoesNotContain(a.Producers, p => p.RecipeName == "家内工業");
                Assert.Equal(a.Pops.Count, b.Pops.Count);   // 行だけを足す＝POP は増えない
                var rest = b.Producers.Where(p => p.RecipeName != "家内工業").ToList();
                Assert.Equal(a.Producers.Count, rest.Count);
                for (int k = 0; k < rest.Count; k++)
                {
                    Assert.Equal(a.Producers[k].GoodId, rest[k].GoodId);
                    Assert.Equal(a.Producers[k].RecipeName, rest[k].RecipeName);
                    Assert.Equal(a.Producers[k].Productivity, rest[k].Productivity);
                    Assert.Equal(a.Producers[k].MarginalCost, rest[k].MarginalCost);
                    Assert.Equal(a.Producers[k].LaborPopIndex, rest[k].LaborPopIndex);
                }
                // 手仕事の行は既存の行の後ろ（先頭の個体〔FindProducer の規約〕は変わらない）
                Assert.True(b.Producers.Take(a.Producers.Count).All(p => p.RecipeName != "家内工業"));
            }
        }
        Assert.True(regions > 10, "前提が崩れた：州が少ない");
    }

    [Fact]
    public void 旗1では本土の全ての州に8本の行が手書きの表どおりに入る()
    {
        int regions = 0;
        foreach (var w in Worlds(1.0)) foreach (var r in w.Regions) { regions++; AssertHomeRows(r); }
        Assert.True(regions > 10);
    }

    [Fact]
    public void 旗1では昇格した植民地の州にも同じ8本が入る()
    {
        var sim = Multi(1.0);
        int colonies = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
            if (r.Producers.Any(p => p.RecipeName == "家内工業")) { colonies++; AssertHomeRows(r); }
        Assert.True(colonies > 10, "前提が崩れた：行を持つ植民地の州が少ない");
        var off = Multi(0.0);
        foreach (var n in off.Nations) foreach (var r in n.World.Regions) Assert.DoesNotContain(r.Producers, p => p.RecipeName == "家内工業");
    }

    [Fact]
    public void 職人の行は括り出した定数でも手計算の値のまま()
    {
        int seen = 0;
        foreach (var w in Worlds(0.0))
            foreach (var r in w.Regions)
                foreach (var p in r.Producers.Where(p => p.RecipeName == "職人" && p.GoodId == Catalog.ClothingId))
                {
                    seen++;
                    Assert.Equal(1.8 * 7.0 * 0.6, p.Productivity, 12);
                    Assert.Equal(0.35, p.MarginalCost); Assert.Equal(30.0, p.Output); Assert.Equal(1.0, p.Scale);
                }
        Assert.True(seen > 5, "前提が崩れた：職人の衣料の行が少ない");
    }

    [Fact]
    public void 発見の門は1で眠らず国営の工場の候補からは外れる()
    {
        int seen = 0;
        foreach (var w in Worlds(1.0))
            foreach (var r in w.Regions)
            {
                var def = r.Producers.First(p => p.GoodId == Catalog.ClothingId && p.RecipeName != "家内工業" && p.RecipeName != "職人");   // 比較の相手＝同じ財の既定の行
                foreach (var row in r.Producers.Where(p => p.RecipeName == "家内工業"))
                {
                    seen++;
                    Assert.Equal(1.0, Discovery.Gate(r, row));
                    Assert.Equal((byte)0, EconomyStep.SleepState(r, row, w.Coef));
                    Assert.True(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, row).Score));
                }
                Assert.False(double.IsNaN(UtilityAgent.FacilityBuildScore(w, r, def).Score), "前提が崩れた：既定の行まで候補外（NaN の理由が述語でなくなった）");
            }
        Assert.True(seen > 10);
    }

    [Fact]
    public void 旗1の短い走行は完走し産出は有限で保存の往復が一致する()
    {
        var sim = Multi(1.0);
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        double homeOut = 0; int rows = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                foreach (var p in r.Producers)
                {
                    Assert.True(double.IsFinite(p.Output) && p.Output >= 0 && double.IsFinite(p.Scale), $"産出が有限でない: 財 {p.GoodId} {p.RecipeName}");
                    if (p.RecipeName == "家内工業") { rows++; homeOut += p.Output; }
                }
                Assert.All(r.Pops, p => Assert.True(double.IsFinite(p.Size) && double.IsFinite(p.Savings.Value)));
            }
        Assert.True(rows > 80);
        Assert.True(homeOut > 0, "手仕事の行がまったく産まない：前提が崩れた");
        using var ms1 = new MemoryStream();
        SaveGame.Save(sim, ms1);
        ms1.Position = 0;
        var sim2 = SaveGame.Load(ms1);
        using var ms2 = new MemoryStream();
        SaveGame.Save(sim2, ms2);
        Assert.Equal(ms1.ToArray(), ms2.ToArray());
        int again = 0;
        foreach (var n in sim2.Nations) foreach (var r in n.World.Regions) again += r.Producers.Count(p => p.RecipeName == "家内工業");
        Assert.Equal(rows, again);
    }
}
