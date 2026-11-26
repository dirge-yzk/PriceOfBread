using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1322) c138sm 計器の列の番人。region_s の末尾の 2 列（soldierK・advLaborShortage）が、試験の中で置いた値のとおりに出る。
/// 期待値は盤面に置いた値から手で組む（計器の関数の中の足し算を写さない）。力学は動かさない（計器の行だけを読む）。</summary>
public class InstrumentColumnTests
{
    [Fact]
    public void RegionRow_兵の規模と高度労働力の足りない量が置いた値のとおりに出る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var r = sim.Nations[0].World.Regions[0];
        Assert.True(r.Pops.Count >= 3, "前提：州の POP が 3 つ未満");
        foreach (var p in r.Pops) p.IsSoldier = false;
        r.Pops[0].IsSoldier = true; r.Pops[0].Size = 12.5;
        r.Pops[1].IsSoldier = true; r.Pops[1].Size = 7.25;
        r.Pops[2].Size = 1000.0;   // 兵でない POP の規模は数えない
        r.Markets[Catalog.AdvLaborId].LastShortage = 3.5;
        var row = Instruments.RegionProdRow(r);
        var head = Instruments.RegionProdHeaderText.Split((char)9);
        Assert.Equal(head.Length, row.Length);
        Assert.Equal("soldierK", head[^2]); Assert.Equal("advLaborShortage", head[^1]);
        Assert.Equal(19.75, row[^2]);
        Assert.Equal(3.5, row[^1]);
        r.Pops[1].IsSoldier = false;   // 兵でなくなれば数えない
        Assert.Equal(12.5, Instruments.RegionProdRow(r)[^2]);
    }

    /// <summary>(1386) c147bb region_s の末尾 7 列の純関数。盤面に置いた値から手で書いた期待値と比べる。</summary>
    [Fact]
    public void RegionExtra_工場労働者の吸い込み_主食の流れ_産出が置いた値のとおりに出る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var r = sim.Nations[0].World.Regions[0];
        // 吸い込み：労働 POP 1 に 3 つ（投入の充足 1.0・0.0・0.04）・労働 POP 2 に 1 つ（多数側でない）
        Producer P(int pop, double lw, double scale, double fill) => new() { GoodId = Catalog.BreadId, BuysLabor = true, LaborPopIndex = pop, LaborWeight = lw, Scale = scale, LastInputFill = fill };
        r.Producers.Clear();
        r.Producers.Add(P(1, 2.0, 1.0, 1.0));    // 乗る重み 2.0・吸い込みでない
        r.Producers.Add(P(1, 3.0, 0.5, 0.0));    // 乗る重み 1.5・吸い込み
        r.Producers.Add(P(1, 4.0, 0.25, 0.04));  // 乗る重み 1.0・吸い込み（0.05 未満）
        r.Producers.Add(P(2, 100.0, 1.0, 0.0));  // 別の POP＝数えない
        Assert.Equal(2.5 / 4.5, Instruments.WorkerSinkShare(r), 12);
        r.Producers[2].LastInputFill = 0.05;      // 閾の上（0.05 ちょうどは吸い込みでない）
        Assert.Equal(1.5 / 4.5, Instruments.WorkerSinkShare(r), 12);
        r.Producers.Clear();
        Assert.True(double.IsNaN(Instruments.WorkerSinkShare(r)));
        // 主食の流れ：パンと米だけ置く（他の財は 0 にする）
        foreach (var m in r.Markets) { m.PrevImport = 0; m.LastProduced = 0; }
        r.Markets[Catalog.BreadId].PrevImport = 2.0; r.Markets[Catalog.BreadId].LastProduced = 10.0;
        r.Markets[Catalog.RiceId].PrevImport = 0.5; r.Markets[Catalog.RiceId].LastProduced = 4.0;
        r.Markets[Catalog.AdvLaborId].PrevImport = 99.0; r.Markets[Catalog.AdvLaborId].LastProduced = 99.0;   // 主食でない
        var (imp, prod) = Instruments.StapleFlows(r);
        Assert.Equal(2.5, imp); Assert.Equal(14.0, prod);
        // 産出：役務の財は数えない
        var bread = new Producer { GoodId = Catalog.BreadId, Output = 3.0 };
        r.Producers.Add(bread); r.Producers.Add(new Producer { GoodId = Catalog.AdvLaborId, Output = 50.0 });
        Assert.Equal(3.0 * r.GoodOf(Catalog.BreadId)!.RefPrice, Instruments.RegionOutputRef(r), 12);
    }

    /// <summary>(1386) sellr_s の inputPricePerUnit：パンの投入（小麦粉 1.0）の地元の値段×量。</summary>
    [Fact]
    public void InputPricePerUnit_投入の地元の値段と名前どおりの量の積()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var r = sim.Nations[0].World.Regions[0];
        r.Markets[Catalog.FlourId].Price = 3.5;
        Assert.Equal(3.5, Instruments.InputPricePerUnitOf(r, Catalog.BreadId));
        r.Markets[Catalog.FlourId].Price = 7.0;
        Assert.Equal(7.0, Instruments.InputPricePerUnitOf(r, Catalog.BreadId));
        Assert.Equal(0.0, Instruments.InputPricePerUnitOf(r, Catalog.RiceId));   // 投入の無い財
    }

    /// <summary>(1386) trrail_s の調達の 4 列：在庫 0・貯蓄で削る・満額の 3 回の調達を置いて、手で書いた件数と量。</summary>
    [Fact]
    public void RailMonthProcured_在庫0と貯蓄で削った回と量が置いたとおりに数える()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var r = w.Regions[0];
        var rm = new Instruments.RailMonth(w, w.Coef);
        var a = Instruments.RailOf(r); a.Clear();
        rm.Procured(r, 10.0, 0.0, 0.0, 2.0, 1000.0);   // 在庫 0
        rm.Procured(r, 10.0, 4.0, 50.0, 2.0, 8.0);     // 貯蓄 8 ＜ min(50,10)×2＝20＝削る
        rm.Procured(r, 5.0, 5.0, 50.0, 2.0, 1000.0);   // 満額
        Assert.Equal(25.0, a.ProcWant); Assert.Equal(9.0, a.ProcGot);
        Assert.Equal(1, a.ProcStockZero); Assert.Equal(1, a.ProcSavBind);
        a.Clear();
        Assert.Equal(0.0, a.ProcWant); Assert.Equal(0, a.ProcSavBind);
    }

    /// <summary>(1386) 見出しの末尾の名前と、計器を回した表の行の列の数＝見出しの列の数（region_s・trrail_s・sellr_s）。</summary>
    [Collection("Instruments")]
    public class Tables
    {
        [Fact]
        public void 足した列は見出しの末尾で行の列の数と合う()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_bb_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*");
            int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 2;
            try
            {
                var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent());
                sim.DiplomacyFrozen = true;
                Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
                try { for (int q = 0; q < 24; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
                finally { Instruments.Close(sim); }
                string[] Tail(string kind, int n, out int rows, out int bad)
                {
                    var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s1789.tsv"));
                    var h = lines[0].Split('\t'); rows = lines.Length - 1; bad = lines.Skip(1).Count(l => l.Split('\t').Length != h.Length);
                    return h[^n..];
                }
                Assert.Equal(new[] { "transportCapEnd", "tradeThrottleEnd", "regionShareEnd", "workerSinkShareEnd", "stapleImportPerMonth", "stapleProducedPerMonth", "gdpOutputShareEnd" }, Tail("region", 7, out int r1, out int b1));
                Assert.Equal(new[] { "procureWantUnitsPerYear", "procureStockZeroGoodMonths", "procureSavingsBindGoodMonths", "procureGotUnitsPerYear", "tPreEnd" }, Tail("trrail", 5, out int r2, out int b2));
                Assert.Equal(new[] { "price", "inputPricePerUnit" }, Tail("sellr", 2, out int r3, out int b3));
                Assert.True(r1 > 100 && r2 > 10 && r3 > 100, $"対照：行が少ない（{r1}・{r2}・{r3}）");
                Assert.Equal(0, b1); Assert.Equal(0, b2); Assert.Equal(0, b3);
            }
            finally { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        }
    }
}
