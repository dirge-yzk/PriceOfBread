using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(513) 恒等の束 hz6 の番人＝畳んだ読み口が、畳む前の式と NaN 以外の全ての入力で同じ値を返すことと、NaN だけが倒れることを、独立に書いた式で確かめる。</summary>
public class Hz6IdentityTests
{
    private static readonly double[] Grid = [double.NegativeInfinity, -1e9, -1.5, -1, -0.0, 0.0, 0.1, 0.3, 0.5, 0.75, 1, 1.0000001, 2, 40, 1e9, double.PositiveInfinity];

    [Fact]
    public void ClampOr_は_NaN_以外で_MathClamp_とビット単位で同じ_NaN_だけ_neutral_へ()
    {
        int n = 0;
        foreach (double lo in new[] { -40.0, -0.0, 0.0, 0.1, 0.3 })
            foreach (double hi in new[] { 0.3, 1.0, 40.0, 1e9 })
            {
                if (lo > hi) continue;
                foreach (double x in Grid)
                {
                    double want = Math.Clamp(x, lo, hi);
                    double got = PoliticalQuantities.ClampOr(x, lo, hi, 123.0);
                    Assert.Equal(BitConverter.DoubleToInt64Bits(want), BitConverter.DoubleToInt64Bits(got));
                    n++;
                }
                Assert.Equal(123.0, PoliticalQuantities.ClampOr(double.NaN, lo, hi, 123.0));
            }
        Assert.True(n > 100);
    }

    [Fact]
    public void AtLeast_は_NaN_以外で_MathMax_とビット単位で同じ_NaN_だけ_floor_へ()
    {
        foreach (double floor in new[] { 0.0, 0.3, -1.0, 1e-9 })
        {
            foreach (double x in Grid)
                Assert.Equal(BitConverter.DoubleToInt64Bits(Math.Max(floor, x)), BitConverter.DoubleToInt64Bits(PoliticalQuantities.AtLeast(x, floor)));
            Assert.Equal(floor, PoliticalQuantities.AtLeast(double.NaN, floor));
        }
    }

    [Fact]
    public void 衛生の読み口は上下水道と石鹸と電化の上乗せを0から1に収めた手書きの式と同じ()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World;
        var c = w.Coef; var r = w.Region;
        foreach (var (inf, soap, elec) in new[] { (1.0, 0.0, 0.0), (0.2, 0.5, 0.1), (0.9, 0.9, 0.9), (0.0, 0.0, 0.0), (0.4, 1.0, 1.0) })
        {
            r.InfraFill = inf; r.SoapFill = soap; r.ElecInfraFill = elec;
            double raw = inf + c.SoapSanitationWeight * soap + c.ElecInfraSanitationWeight * elec;
            double want = raw < 0 ? 0 : raw > 1 ? 1 : raw;
            Assert.Equal(want, r.Sanitation(c));
        }
    }

    [Fact]
    public void 労働の財の述語は労働と高度労働と農業労働の3つだけ()
    {
        var want = new[] { 84, 91, 92 };   // 番号の生の値（Catalog の定数から作らない）
        for (int id = -2; id < Catalog.Goods.Length + 2; id++)
            Assert.Equal(want.Contains(id), Catalog.IsLaborGood(id));
    }

    [Fact]
    public void 熟練は1本でNavySkillは同じ値を読む()
    {
        var w = new Simulation(NationConfig.France().ToScenario(155), new NullAgent()).World;
        var c = w.Coef;
        w.ArmySkill = 0.4; w.MilitaryTrainingFill = 1.0;
        Military.StepSkill(w, c, atWar: true);
        Assert.True(w.ArmySkill > 0.4);
        Assert.Equal(w.ArmySkill, w.NavySkill);
    }

    [Fact]
    public void 在庫循環の乗数は不足で1より上_過剰で下_ゲイン0なら1()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World;
        var c = w.Coef; var r = w.Region; int g = Catalog.BreadId;
        var m = r.Markets[g];
        m.LastDemand = 100;
        m.Stock = 0;
        double lack = EconomyStep.InventoryAdjust(r, g, c);
        m.Stock = 1e6;
        double glut = EconomyStep.InventoryAdjust(r, g, c);
        Assert.True(lack > 1.0 && glut < 1.0);
        // 独立の手計算（不足＝在庫0 なら gap=+1 → 1+gain）
        Assert.Equal(1.0 + c.InventoryAdjustGain, lack, 6);
        c.InventoryAdjustGain = 0;
        Assert.Equal(1.0, EconomyStep.InventoryAdjust(r, g, c));
    }
}
