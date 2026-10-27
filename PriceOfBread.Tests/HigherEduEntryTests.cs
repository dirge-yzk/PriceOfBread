using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(597) 線 sch2 段1：高等教育の入口（<c>ResearcherFrac</c>＝研究者 POP と大学の生産者・<c>HigherEduNeedPerEducated</c>＝識字した POP の必要量）の機構の番人。
/// 入口は既定で 0（二重の休眠）＝**既定の世界には買い手も売り手も居ない**。腕 B（<c>POB_COEF</c> で 2 係数を立てる）で入口が開くことを、構築した盤面と短い走りで確かめる。
/// 既定を 1 ビットも動かさない（重なりの読み (585) 柵 2）ので、ここに入るのは番人だけ。期待値は盤面に自分で置いた数（13 本・14 本・係数の値）から作る。
/// 事前登録 measurements/sch2_597_20261027_prereg.txt [1] 段1・[3] K1。</summary>
public class HigherEduEntryTests
{
    private static Simulation Make(double researcherFrac, double need)
    {
        var sc = NationConfig.Britain().ToScenario(41);
        var coef = new Coefficients { ResearcherFrac = new Share(researcherFrac), HigherEduNeedPerEducated = need };
        typeof(Scenario).GetProperty("Coef")!.SetValue(sc, coef);   // Scenario.Coef は init 専用（構築時に読む係数 ResearcherFrac を州の構築へ届ける）
        return new Simulation(sc, new NullAgent());
    }

    [Fact]
    public void 既定の世界には研究者も大学も買い手も居ない()
    {
        Assert.Equal(0.0, new Coefficients().ResearcherFrac.Value);
        Assert.Equal(0.0, new Coefficients().HigherEduNeedPerEducated);
        var sim = Make(0.0, 0.0);
        foreach (var r in sim.World.Regions)
        {
            Assert.Equal(-1, r.ResearcherPopIndex);
            Assert.DoesNotContain(r.Producers, p => p.GoodId == Catalog.HigherEducationId);
        }
        sim.Run(1);
        foreach (var r in sim.World.Regions)
        {
            Assert.Equal(0.0, r.Markets[Catalog.HigherEducationId].LastSold);
            foreach (var p in r.Pops) { Assert.Equal(0.0, p.HigherEduFill); }
        }
    }

    [Fact]
    public void 入口を開けると研究者と大学が現れ_識字した者が高等教育を買える_人口は保存される()
    {
        var off = Make(0.0, 0.0);
        var on = Make(0.05, 0.05);
        double Pop(Simulation s) => s.World.Regions.Sum(r => r.Pops.Sum(p => p.Size));
        Assert.Equal(Pop(off), Pop(on), 6);   // 研究者は教師・官僚・医者の取り分から切り出す＝新しい人は湧かない
        var cap = on.World.Region;
        Assert.True(cap.ResearcherPopIndex >= 0, "入口を開けても研究者 POP が現れない");
        Assert.Equal(off.World.Region.Pops.Count + 1, cap.Pops.Count);   // 研究者の POP が 1 件増えるだけ（階級の名簿の件数に縛らない）
        Assert.Equal(PopClass.Researcher, cap.Pops[cap.ResearcherPopIndex].ClassId);
        Assert.True(cap.Pops[cap.ResearcherPopIndex].Size > 0, "研究者 POP の規模が 0");
        var uni = cap.Producers.FirstOrDefault(p => p.GoodId == Catalog.HigherEducationId);
        Assert.NotNull(uni);
        Assert.Equal(cap.ResearcherPopIndex, uni!.LaborPopIndex);   // 大学の働き手は研究者
        on.Run(2);   // 2 四半期＝6 回の月次決算（CheckInvariants が毎月走る＝例外が出ないこと自体が貨幣・人口の保存の確かめ）
        double sold = on.World.Regions.Sum(r => r.Markets[Catalog.HigherEducationId].LastSold);
        double fill = on.World.Regions.Sum(r => r.Pops.Sum(p => p.HigherEduFill * p.Size));
        Assert.True(sold > 0, "入口を開けても高等教育が 1 単位も売れていない（K1：入口が開いていない）");
        Assert.True(fill > 0, "入口を開けても高等教育の充足が立たない");
    }
}
