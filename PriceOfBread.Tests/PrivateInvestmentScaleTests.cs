using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (300) 線 i1：私的な建設 1 束が上げる工業化度 <c>EconomyStep.PrivateIndPerConstruction</c>（<c>IndCapitalOutputYears</c>＝κ）の番人。
/// 期待は式の写しでなく<b>関係</b>で書く（州の所得が 2 倍なら 1 束の工業化度は半分・物価水準 2 倍なら 2 倍・κ 2 倍なら半分・κ=0 は IndPerConstruction のまま・所得 0 は今のまま）。
/// 絶対値の 1 点は本体の式（基準の束の費用 BundleRef の 4 項と N の式）をテストの中へ写した物＝本体と同じ出所の同語反復で、効いているのは関係の Assert（(301) 反証役 i1〔v〕低#4）。配線（決済が同じ 1 束の工業化度を読む）は InstrumentsTests.私的な建設の工業化度の累積は建った束と1束の工業化度の積に閉じる が見る。
/// </summary>
public class PrivateInvestmentScaleTests
{
    private static (Region Region, Coefficients Coef) Make()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        var region = sim.World.Regions.First(r => r.InvestorPopIndex >= 0);
        return (region, sim.World.Coef);
    }

    private static void SetIncome(Region r, double perMonthTotal)
    {
        var pops = r.Pops;
        foreach (var p in pops) p.LastEarnedPerMonth = new MoneyPerMonth(perMonthTotal / pops.Count);
    }

    [Fact]
    public void 工業化度の束あたりの上がりは州の実質の所得に反比例する()
    {
        var (r, c) = Make();
        // κ=0（既定）＝今のまま
        c.IndCapitalOutputYears = 0;
        SetIncome(r, 5000);
        Assert.Equal(c.IndPerConstruction, EconomyStep.PrivateIndPerConstruction(r, c));

        c.IndCapitalOutputYears = 1.0;
        r.PriceLevel = 1.0;
        SetIncome(r, 5000);
        double a = EconomyStep.PrivateIndPerConstruction(r, c);
        // 絶対値の 1 点（本体の式の写し）：N＝κ×年所得÷（基準の束の費用×(1+工賃の割合)×物価水準）。基準の束の費用は財の基準価格の積の和をここで足し直す。
        double bundleRef = c.BuildingGoodBundle > 0   // ★(555) 既定の束は建造物＋機械（旗 0 は鋼・機械・セメント・製材）＝今の束の財と量の積の和
            ? c.ConsBuilding * Catalog.RefPrices[Catalog.BuildingId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]
            : c.ConsSteel * Catalog.RefPrices[c.SteelGoodId] + c.ConsMachine * Catalog.RefPrices[c.MachineGoodId]
            + c.ConsCement * Catalog.RefPrices[c.CementGoodId] + c.ConsLumber * Catalog.RefPrices[c.LumberGoodId];
        double n = 1.0 * (5000.0 * 12.0) / (bundleRef * (1.0 + c.ConstructionLaborWageFrac) * 1.0);
        Assert.True(Math.Abs(a - 1.0 / n) <= 1e-12 * (1.0 / n), $"1 束の工業化度 {a} が 1÷N {1.0 / n} に閉じない");

        SetIncome(r, 10000);                       // 所得 2 倍 → 半分
        Assert.True(Math.Abs(EconomyStep.PrivateIndPerConstruction(r, c) - a / 2) <= 1e-12 * a);
        SetIncome(r, 5000);
        r.PriceLevel = 2.0;                        // 物価水準 2 倍（実質の所得が半分）→ 2 倍
        Assert.True(Math.Abs(EconomyStep.PrivateIndPerConstruction(r, c) - a * 2) <= 1e-12 * a);
        r.PriceLevel = 1.0;
        c.IndCapitalOutputYears = 2.0;             // κ 2 倍 → 半分
        Assert.True(Math.Abs(EconomyStep.PrivateIndPerConstruction(r, c) - a / 2) <= 1e-12 * a);
        c.IndCapitalOutputYears = 1.0;
        SetIncome(r, 0);                           // 所得 0 → 今のまま（フェイルオープン）
        Assert.Equal(c.IndPerConstruction, EconomyStep.PrivateIndPerConstruction(r, c));
    }

    /// <summary>既定の宣言（(300) 腕 B を採った＝資本産出比 κ の既定 1.0）。覆すなら理由とともにここを書き換える。</summary>
    [Fact]
    public void 資本産出比の既定は1年分()
    {
        Assert.Equal(1.0, new Coefficients().IndCapitalOutputYears);
    }
}
