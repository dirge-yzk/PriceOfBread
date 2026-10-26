using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(490) 計算量の台帳の「年齢の層の歩」の行（POP×月×〔層の数−8〕）の番人＝機構と不変条件。
/// 8層の年齢の算術は月次・消費の単価の中に既にある＝この行は 11 層の増分だけ（8層は 0 件・二重に数えない）。単価は上限の見積り（census 無し・出所は <see cref="CostLedger.AgeBandExtraUnitMicros"/> の注）。
/// 期待値は別の経路から作る：件数は盤面の州の POP の数（独立に数える）×月数×（層の数−8）／推定は件数×単価の手の算術。
/// ⚠層の数はプロセスの環境変数 POB_AGE_BANDS で決まる（既定 11・8 で旧）＝この番人は両方のプロセスで回す（8 のとき件数 0）。</summary>
public class AgeLedgerTests
{
    [Fact]
    public void 年齢の層の歩の行は台帳の表に揃い単価は正の上限の見積り()
    {
        Assert.Equal(CostLedger.Count, CostLedger.Names.Length);
        Assert.Equal(CostLedger.Count, CostLedger.UnitMicros.Length);
        Assert.True(CostLedger.AgeBandExtraPops < CostLedger.Count);
        Assert.Equal(CostLedger.AgeBandExtraUnitMicros, CostLedger.UnitMicros[CostLedger.AgeBandExtraPops]);
        Assert.True(CostLedger.AgeBandExtraUnitMicros > 0);
        Assert.Contains("年齢の層", CostLedger.Names[CostLedger.AgeBandExtraPops]);
        // 推定は件数×単価の手の算術（他の行は 0）
        var n = new long[CostLedger.Count]; n[CostLedger.AgeBandExtraPops] = 25_000_000;
        Assert.Equal(25_000_000 * CostLedger.AgeBandExtraUnitMicros * 1e-6, CostLedger.EstimatedSeconds(n), 9);
    }

    [Fact]
    public void 年齢の層の歩の件数は州のPOPの数かける月数かける層の数マイナス8()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        long monthly0 = w.Cost[CostLedger.MonthlyPops], extra0 = w.Cost[CostLedger.AgeBandExtraPops];
        // 独立の数え：活きている州の POP の数（1 四半期の間は増減しない前提を確かめる）
        int popsBefore = w.Regions.Sum(r => r.Pops.Count);
        sim.StepQuarter();
        int popsAfter = w.Regions.Sum(r => r.Pops.Count);
        Assert.Equal(popsBefore, popsAfter);   // 前提：この盤面では 1 四半期の間に POP の数が変わらない
        long monthly = w.Cost[CostLedger.MonthlyPops] - monthly0, extra = w.Cost[CostLedger.AgeBandExtraPops] - extra0;
        Assert.True(monthly > 0, "前提が崩れた：月次の POP の件数が立たない");
        Assert.Equal((long)popsBefore * 3, monthly);   // 1 四半期＝3 か月（独立に数えた POP の数×3）
        Assert.Equal(monthly * (Pop.Layout.Bands - 8), extra);   // 8 層では 0・11 層では 3 倍
        if (Pop.Layout.Bands == 8) Assert.Equal(0L, extra);
    }
}
