using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1336) c140sp 旗 StateProcureAsSold の番人（事前登録 measurements/m1336_20261124_prereg_sp.txt の G1 の T1〜T5）。
/// 国家の購入（InternationalMarket.Procure の buyer null＝Colonial.BuyUpkeepGood）は LastDemand に want を積み、旧は buy を LastExport にも積んだ＝値段の式
/// （MarketFlow の需要の側＝LastDemand＋LastExport）が国家の需要を 2 回数えた。旗 1 は buy を LastSold に積む（造幣の引き取りと同じ）。旗は盤面で置く。
/// 突然変異（scripts/instruments/c140sp/mut_sp.sh）：μ0 旗を読まない→T1・T2 赤／μ1 旗 1 も LastExport→T1・T2 赤／μ2 両方に積む→T1・T2・T3 赤／μ3 buyer ありも LastExport→T4 赤／
/// μ4 計器が Tick で決済の月を戻さない→T5 赤／μ5 計器の新の規則の符号→T5 赤。</summary>
[Collection("Instruments")]
public class StateProcureLedgerTests
{
    const int G = Catalog.CoalId;

    /// <summary>英の盤面を 2 日回し、石炭の市場を値段の式が動く形に置く（参照の値段・需要＝産出 10・在庫 20・前の月の輸出 0）。</summary>
    private static Simulation Board(double flag)
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);
        s.World.Coef.StateProcureAsSold = flag;
        var r = s.World.Region;
        Assert.Contains(r.EnsureLiveGoods(), g => g.Id == G);   // 前提：石炭の市場が値段の式の対象
        var m = r.Markets[G];
        m.FlowAsleep = false;
        m.Price = Catalog.RefPrices[G] * r.PriceLevel; m.LastDemand = 10.0; m.LastProduced = 10.0; m.LastImport = 0; m.LastSold = 10.0; m.LastExport = 0; m.Stock = 20.0;
        return s;
    }

    [Fact]
    public void T1_旗1の国家の購入は輸出でなく販売に積む()
    {
        var s = Board(1.0); var w = s.World; var m = w.Region.Markets[G];
        double ex0 = m.LastExport, sold0 = m.LastSold, dem0 = m.LastDemand, st0 = m.Stock;
        double got = InternationalMarket.Procure(w, w.Region, G, 5.0, null, TreasuryCat.ColonialUpkeep);
        Assert.Equal(5.0, got);   // 手の値：在庫 20 から want 5 を全て買える
        Assert.Equal(BitConverter.DoubleToInt64Bits(ex0), BitConverter.DoubleToInt64Bits(m.LastExport));
        Assert.Equal(sold0 + 5.0, m.LastSold);
        Assert.Equal(dem0 + 5.0, m.LastDemand);   // want は今のまま LastDemand へ
        Assert.Equal(st0 - 5.0, m.Stock);
    }

    /// <summary>値段の式は国家の需要を 1 回だけ数える：旗 1 の Procure の後の値段は、同じ盤面で LastDemand に want を 1 回足し在庫を buy だけ減らした対照とビットで同じ。
    /// 旗 0 は LastExport の分だけ需要の側が増える＝対照と違う（盤面が判別できることの確かめ）。</summary>
    [Fact]
    public void T2_値段の式は国家の需要を1回だけ数える()
    {
        double PriceAfter(double flag, bool viaProcure)
        {
            var s = Board(flag); var w = s.World; var r = w.Region; var m = r.Markets[G];
            if (viaProcure) InternationalMarket.Procure(w, r, G, 5.0, null, TreasuryCat.ColonialUpkeep);
            else { m.LastDemand += 5.0; m.Stock -= 5.0; }
            MarketFlow.StepDay(r, w.Coef, 10, null, true);
            return m.Price;
        }
        double control = PriceAfter(1.0, false);
        double on = PriceAfter(1.0, true);
        double off = PriceAfter(0.0, true);
        Assert.Equal(BitConverter.DoubleToInt64Bits(control), BitConverter.DoubleToInt64Bits(on));
        Assert.True(off > control, $"前提が崩れた：旗 0 の値段 {off} が対照 {control} より高くない（盤面が二重を拾えない）");
    }

    /// <summary>在庫の恒等式（Invariants の GoodsBalance）は旗に依らず保たれる（販売と輸出の両方を引く）。台帳を空にした盤面で Procure を 1 回。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T3_在庫の恒等式は旗に依らず保たれる(double flag)
    {
        var s = Board(flag); var w = s.World;
        int n = 0;
        foreach (var r in w.Regions) foreach (var m in r.Markets) { m.PrevStock = m.Stock; m.LastProduced = m.LastImport = m.LastSold = m.LastExport = 0; n++; }
        Assert.True(n > 10, "前提が崩れた：市場が少なすぎる");
        double got = InternationalMarket.Procure(w, w.Region, G, 5.0, null, TreasuryCat.ColonialUpkeep);
        Assert.True(got > 0, "前提が崩れた：買えていない");
        new GoodsBalance().Check(w);
    }

    /// <summary>buyer あり（貯蓄の足りる POP）の購入は旗に依らず同じ台帳（LastSold へ・LastExport は動かない）。</summary>
    [Fact]
    public void T4_買い手ありの購入は旗に依らない()
    {
        (long sold, long ex, long st) Arm(double flag)
        {
            var s = Board(flag); var w = s.World; var r = w.Region; var m = r.Markets[G];
            var buyer = r.Pops.OrderByDescending(p => p.Savings.Value).First();
            Assert.True(buyer.Savings.Value > 10.0 * m.Price, "前提が崩れた：買い手の貯蓄が足りない");
            double got = InternationalMarket.Procure(w, r, G, 5.0, buyer);
            Assert.Equal(5.0, got);
            return (BitConverter.DoubleToInt64Bits(m.LastSold), BitConverter.DoubleToInt64Bits(m.LastExport), BitConverter.DoubleToInt64Bits(m.Stock));
        }
        var a0 = Arm(0.0); var a1 = Arm(1.0);
        Assert.Equal(a0, a1);
        Assert.Equal(BitConverter.DoubleToInt64Bits(0.0), a1.ex);
    }

    private static Dictionary<string, string> Row(World w)
    {
        var h = Instruments.StProcHeaderText.Split('\t');
        var v = ("0\t" + Instruments.StProcRow(w, 0)).Split('\t');
        Assert.Equal(h.Length, v.Length);
        return h.Zip(v).ToDictionary(x => x.First, x => x.Second);
    }

    private static double D(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>計器の書式は有効数字 10 桁（Instruments.F の G10）＝相対の許し 1e-8（scale は比べる量の桁）。</summary>
    private static void Near(double e, double a, double scale, string what) => Assert.True(Math.Abs(e - a) <= 1e-8 * Math.Max(1.0, Math.Abs(scale)), $"{what}：{e} ≠ {a}");

    /// <summary>計器 stproc_s の行：代金の和・旧の規則と新の規則の差＝決済の月の値打ち・今の輸出の値打ち＝試験が自分で数えた Σ LastExport×Price。
    /// 同じ Tick に 2 回・Tick を進めて 1 回買う（決済の月は Tick で戻る）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T5_計器stproc_sの行は台帳と同じ盤面の旧新に一致する(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sp_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var s = Board(flag); var w = s.World; var r = w.Region; var m = r.Markets[G];
            Row(w);   // 前の数えを空にする
            double px = m.Price;
            InternationalMarket.Procure(w, r, G, 3.0, null, TreasuryCat.ColonialUpkeep);
            InternationalMarket.Procure(w, r, G, 2.0, null, TreasuryCat.ColonialUpkeep);
            w.Tick++;
            InternationalMarket.Procure(w, r, G, 4.0, null, TreasuryCat.ColonialUpkeep);
            double own = 0;
            foreach (var rr in w.Regions) foreach (var mm in rr.Markets) own += mm.LastExport * mm.Price;
            var v = Row(w);
            Assert.Equal(flag > 0 ? "1" : "0", v["asSoldFlag"]);
            Assert.Equal("3", v["procCount"]);
            Near(9.0 * px, D(v["buyValueQ"]), 9.0 * px, "buyValueQ");
            Assert.Equal("1", v["settleTickMatchFlag"]);
            Near(4.0 * px, D(v["settleBuyValueNow"]), 4.0 * px, "settleBuyValueNow");
            Near(own, D(v["exportValueNow"]), own, "exportValueNow");
            Near(4.0 * px, D(v["exportValueOldRuleNow"]) - D(v["exportValueNewRuleNow"]), own + 4.0 * px, "旧−新");
            Near(own, D(v[flag > 0 ? "exportValueNewRuleNow" : "exportValueOldRuleNow"]), own, "旗の側の規則＝今");
            var v2 = Row(w);   // 四半期の数えは戻る
            Assert.Equal("0", v2["procCount"]);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
