using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class InvariantTests
{
    [Fact]
    public void 放置シミュは全ティックで不変条件を保つ()
    {
        var sim = new Simulation(new Scenario { Seed = 5 }, new NullAgent());

        // 例外が飛ばなければ §11.6 は全ティックで保持されている
        var ex = Record.Exception(() => sim.Run(400));

        Assert.Null(ex);
    }

    [Fact]
    public void 自作農シナリオでは総マネーがビット単位で一定()
    {
        var sim = new Simulation(new Scenario { Seed = 5 }, new NullAgent());
        double before = sim.World.TotalMoney;
        sim.Run(400);
        // 自営＝全取りで net=0 のため厳密に一定
        Assert.Equal(before, sim.World.TotalMoney);
        Assert.True(Math.Abs(sim.World.TotalPopulation - (1000d + sim.World.PopulationDelta)) < 1e-3, "人口保存の破れ（基準＋出生死亡）");
    }

    [Fact]
    public void 保存則違反は即座に検出される()
    {
        // 不変条件が「壊れたとき確かに発火する」ことの自己検証。
        var sim = new Simulation(new Scenario { Seed = 9 }, new NullAgent());
        var inv = new MoneyConservation(sim.World);

        sim.World.Region.Pops[0].Savings += 1.0;   // 人為的に総マネーを変える

        Assert.Throws<InvariantViolationException>(() => inv.Check(sim.World));
    }

    /// <summary>M7（2026-09-11）：pop への配分の唯一定義＝World.Distribute（旧 Diplomacy の同文複製を削除・2026-09-28 に Colonial.DistributeToPops もここへ畳んだ＝全POP×頭数は呼び手が集合を名乗る）。
    /// Σ⊿Savings==amount（保存）・受け手不在（総人口0）で amount をそのまま返す・Seceded 州の pop には配らない（AllPops 基準）・
    /// 1e-12 以下と NaN は配らず 0 を返す（否定形ガード＝Invariants の許容 1e-8×scale 未満）。</summary>
    [Fact]
    public void popへの配分は保存し受け手不在は返し分離州には配らない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        Region? other = null;
        foreach (var r in w.Regions) if (!ReferenceEquals(r, w.Region) && r.SumPopulation() > 1 && (other is null || r.SumPopulation() > other.SumPopulation())) other = r;
        Assert.NotNull(other);
        double SumSavings() { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) s += p.Savings; return s; }
        double OtherSavings() { double s = 0; foreach (var p in other!.Pops) s += p.Savings; return s; }

        double before = SumSavings();
        Assert.Equal(0.0, World.Distribute(new List<Pop>(w.AllPops()), 1000.0));
        Assert.True(Math.Abs(SumSavings() - before - 1000.0) <= 1e-6, $"配分が保存しない（Σ⊿Savings={SumSavings() - before}）");

        other!.Seceded = true;
        double otherBefore = OtherSavings(), before2 = SumSavings();
        Assert.Equal(0.0, World.Distribute(new List<Pop>(w.AllPops()), 500.0));
        Assert.Equal(otherBefore, OtherSavings());   // 分離凍結中の pop は受け手でない
        Assert.True(Math.Abs(SumSavings() - before2 - 500.0) <= 1e-6, "分離州を除いても保存する");
        other.Seceded = false;

        double before3 = SumSavings();
        Assert.Equal(0.0, World.Distribute(new List<Pop>(w.AllPops()), 1e-13));
        Assert.Equal(0.0, World.Distribute(new List<Pop>(w.AllPops()), double.NaN));
        Assert.Equal(before3, SumSavings());   // 微小/NaN は1銭も動かさない

        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Size = 0;
        Assert.Equal(700.0, World.Distribute(new List<Pop>(w.AllPops()), 700.0));   // 受け手不在＝配れなかった額をそのまま返す（呼び手が出所へ戻す）
    }

    /// <summary>NaN破綻の番人（2026-09-21）：★本国を全て失い植民地だけが残った国の形を固定する。
    /// 現金が尽きた州では州合計の貯蓄が浮動小数の残差で極小の**負値**になる（実測＝S1 の腕＝当時の旗 PoliticalCapitalStock=1（2026-09-21 ㉖で既定へ焼き込み）・seed6 の1831年、
    /// 本国州を全部失い植民地3州へ縮んだスペインのエルパソ州で rSav=−8.3e-16＝資本家popの−8.9e-16 が残っただけ）。
    /// 地域物価乗数は (州の1人あたり貯蓄 / 国の1人あたり貯蓄)^RegionalPriceLevelWeight なので、
    /// Math.Pow(負, 0.3)=NaN・Math.Clamp は NaN を除去しない＝州物価→MarketFlow の価格→専売益→国庫→popの貯蓄 と
    /// NaN が伝播し「非負境界：貯蓄[貴族] が NaN」で落ちていた。否定形ガードで辺境デフレの底へ退避する。</summary>
    [Fact]
    public void 現金が尽きた州の物価乗数はNaNにならない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(6), new NullAgent());
        var w = sim.World;
        Region? poor = null;
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active && !ReferenceEquals(r, w.Region) && r.Pops.Count > 0 && r.SumPopulation() > 1) { poor = r; break; }
        Assert.NotNull(poor);

        // 破綻した州＝全popの現金が尽き、1popにだけ丸め残差の負値が残る（実測と同じ桁）
        foreach (var p in poor!.Pops) p.Savings = 0;
        poor.Pops[0].Savings = -8.881784197001252e-16;
        double natSav = 0, natPop = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) foreach (var p in r.Pops) { natSav += p.Savings; natPop += p.Size; }
        Assert.True(natSav / natPop > 1e-9, "比の分母（国の1人あたり貯蓄）が生きている前提＝分子だけが負に転ぶ形");

        sim.UpdateRegionalPriceLevels();

        Assert.False(double.IsNaN(poor.LocalPriceMultiplier) || double.IsInfinity(poor.LocalPriceMultiplier), "州物価乗数が NaN／∞");
        Assert.Equal(w.Coef.RegionalPriceLevelMin, poor.LocalPriceMultiplier);   // 現金ゼロ＝国平均に対して最も薄い＝辺境デフレの底（rPerCap==0 の既存経路と同じ答え）

        // 伝播の番人：この盤面から1日進めても、州物価→価格→国庫→貯蓄のどこにも NaN／∞ が出ない。
        // ⚠植えた残差そのもの（負の貯蓄）は非負境界が正しく咎めるので、ここで見るのは**有限性だけ**
        //   （実機では月末の BackstopNegativeSavings の後に残差が生まれ、次の検査までに NaN へ化けていた）。
        sim.StepDay();
        static bool Bad(double v) => double.IsNaN(v) || double.IsInfinity(v);
        foreach (var r in w.Regions)
        {
            Assert.False(Bad(r.LocalPriceMultiplier) || Bad(r.PriceLevel), $"州物価が NaN／∞（{r.Name}）");
            foreach (var m in r.Markets) Assert.False(Bad(m.Price) || Bad(m.Stock), $"価格／在庫が NaN／∞（{r.Name}）");
            foreach (var p in r.Pops) Assert.False(Bad(p.Savings) || Bad(p.Size), $"貯蓄／規模が NaN／∞（{r.Name}・{p.ClassName}）");
        }
        Assert.False(Bad(w.Treasury) || Bad(w.PriceLevel), "国庫／物価水準が NaN／∞");
    }
}
