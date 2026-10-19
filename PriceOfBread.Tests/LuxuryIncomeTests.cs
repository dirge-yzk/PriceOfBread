using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(329) 線(2)(3)（取り込み元 (302)）：奢侈品と家事奉仕の欲しい量の所得弾力（階級単位）の番人。
/// 係数 <c>LuxuryIncomeElasticity</c>＝ε・<c>DomesticServiceUsesIncomeElasticity</c>＝家事奉仕を同じ式に入れる切り替え（★(436) 注：(433) 以後は 1 以外で f に掛ける指数・既定 2）。
/// 世界の1人あたり実質所得＝<see cref="World.WorldRealIncomePerCapitaPerMonth"/>（MultiNationSim が毎月配る）・買い手の POP の r＝(前の月の確定した所得÷物価÷人数)÷世界の値。
/// 期待値は手の算術（コードの式を呼ばない・同じ出所から作らない）。事前登録＝measurements/de_20261014_prereg.txt（取り込み元 measurements/s2_20261013_prereg.txt）。
/// 突然変異（実装を一時的に壊して赤を確かめる・結果は実装ログ (329)）：①人口加重を単純平均に ②為替を掛ける（(299) の二重の割り算の形）③物価で割らない ④二重に割る
/// ⑤国単位の r（全 POP が同じ r）に ⑥家事奉仕を対象から外す ⑦切り替えを無視して家事奉仕に常に掛ける ⑧アヘンを除く条件を外す ⑨否定形の守りを外す ⑩計器の奢侈品の輸出額にアヘンを含める
/// ⑪分子を上書きの後の欄にする（分子と分母が別の月）。</summary>
public class LuxuryIncomeTests
{
    private static Pop FirstActivePop(World w) => w.Regions.First(r => r.State == RegionState.Active).Pops[0];

    /// <summary>3国の盤面：全ての POP を人数 0・所得 0 にして、国 A・B の最初の POP だけに人数と所得を置く（世界の平均を手で計算できる）。</summary>
    private static MultiNationSim ToyWorld(out World a, out World b, double incomeA, double sizeA, double incomeB, double sizeB)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        foreach (var n in sim.Nations) foreach (var p in n.World.AllPops()) { p.Size = 0; p.LastEarnedPerMonth = new MoneyPerMonth(0); }
        a = sim.Nations[0].World; b = sim.Nations[1].World;
        var pa = FirstActivePop(a); pa.Size = sizeA; pa.LastEarnedPerMonth = new MoneyPerMonth(incomeA);
        var pb = FirstActivePop(b); pb.Size = sizeB; pb.LastEarnedPerMonth = new MoneyPerMonth(incomeB);
        return sim;
    }

    [Fact]
    public void 世界の1人あたり実質所得は名目を物価で割った人口加重_為替は掛けない_手の算術()
    {
        // 国A：所得 100・人口 10・物価 1。国B：所得 100・人口 40・物価 2・為替 3。
        // 実質（名目÷物価）：A＝100・B＝50。世界＝（100＋50）÷（10＋40）＝3（人口加重）。
        // 為替を掛けると（100＋100×3÷2）÷50＝5・物価を割らないと（100＋100）÷50＝4・為替を掛けて物価で割ると（100＋150）÷50＝5、二重に割ると（100＋25）÷50＝2.5。単純平均でもない。
        var sim = ToyWorld(out var a, out var b, 100, 10, 100, 40);
        a.ExchangeRate = 1.0; a.PriceLevel = 1.0; b.ExchangeRate = 3.0; b.PriceLevel = 2.0;
        sim.DistributeWorldRealIncome();
        Assert.Equal(3.0, a.WorldRealIncomePerCapitaPerMonth, 12);
        Assert.Equal(3.0, b.WorldRealIncomePerCapitaPerMonth, 12);
        Assert.Equal(3.0, sim.Nations[2].World.WorldRealIncomePerCapitaPerMonth, 12);   // 人口 0 の国にも同じ世界の値が配られる
        // 国B の物価が 4 なら実質は 25 ⇒ 世界＝（100＋25）÷50＝2.5。
        b.PriceLevel = 4.0;
        sim.DistributeWorldRealIncome();
        Assert.Equal(2.5, a.WorldRealIncomePerCapitaPerMonth, 12);
    }

    [Fact]
    public void 世界の所得が正でないときと人口0のときは0を配る_買う側が中立へ倒す()
    {
        var sim = ToyWorld(out var a, out var b, 0, 10, 0, 40);   // 所得が全て 0
        a.PriceLevel = 1.0; b.PriceLevel = 1.0;
        sim.DistributeWorldRealIncome();
        Assert.Equal(0.0, a.WorldRealIncomePerCapitaPerMonth);
        var sim2 = ToyWorld(out var a2, out var b2, 100, 0, 100, 0);   // 人口が全て 0
        sim2.DistributeWorldRealIncome();
        Assert.Equal(0.0, a2.WorldRealIncomePerCapitaPerMonth);
        // 物価が NaN の国は分子にも分母にも入れない（非有限）。
        var sim3 = ToyWorld(out var a3, out var b3, 100, 10, 100, 40);
        a3.PriceLevel = 1.0; b3.PriceLevel = double.NaN;
        sim3.DistributeWorldRealIncome();
        Assert.Equal(10.0, a3.WorldRealIncomePerCapitaPerMonth, 12);
    }

    [Fact]
    public void ε0では配りが走らず値は0のまま_ε正では走って件数が立ち全国で同じ値()
    {
        var sim0 = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], seed: 1789, coef: new Coefficients { LuxuryIncomeElasticity = 0 });   // ★(331) 既定は 0.5（点火）＝ε0 は明示
        foreach (var n in sim0.Nations) { n.TelemetryEnabled = false; n.World.Coef.LuxuryIncomeElasticity = 0; }   // ★(331) 国の係数は国ごと別インスタンス（cfg.ToScenario が既定を作る）＝ε0 は国の側にも置く
        sim0.StepQuarter(); sim0.StepQuarter();
        Assert.All(sim0.Nations, n => Assert.Equal(0.0, n.World.WorldRealIncomePerCapitaPerMonth));
        Assert.Equal(0L, sim0.CostTotals()[CostLedger.LuxuryRatioPops]);

        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], seed: 1789, coef: new Coefficients { LuxuryIncomeElasticity = 0.5 });
        foreach (var n in sim1.Nations) n.TelemetryEnabled = false;
        sim1.StepQuarter(); sim1.StepQuarter();
        Assert.True(sim1.CostTotals()[CostLedger.LuxuryRatioPops] > 0, "ε>0 なのに配りの走査が数えられていない＝配りが走っていない（主張が空洞）");
        double y = sim1.Nations[0].World.WorldRealIncomePerCapitaPerMonth;
        Assert.True(y > 0);
        Assert.All(sim1.Nations, n => Assert.Equal(y, n.World.WorldRealIncomePerCapitaPerMonth));   // 世界の値は全国で同じ
    }

    /// <summary>1か国の盤面で EconomyStep.Run を1回。世界の値 Y（既定 10）・物価 2。各 POP の前の月の所得を <c>r×Y×人数×物価</c> にして、その POP の r を手で決める
    /// （既定は貴族 0.25・農民 4・他は 1）。予算が拘束しないよう全ての POP の貯蓄を大きくする。</summary>
    private static (Region Region, World World) OneMonth(double epsilon, double serviceSwitch, double worldY = 10.0, Func<Pop, double>? rOf = null, bool bigCurrentEarned = false)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.LuxuryIncomeElasticity = epsilon;
        w.Coef.DomesticServiceUsesIncomeElasticity = serviceSwitch;
        w.WorldRealIncomePerCapitaPerMonth = worldY;
        w.PriceLevel = 2.0;
        rOf ??= p => p.ClassId == PopClass.Noble ? 0.25 : p.ClassId == PopClass.Peasant ? 4.0 : 1.0;
        foreach (var p in w.AllPops())
        {
            p.SetSavings(1e12);
            p.LastEarnedPerMonth = new MoneyPerMonth(rOf(p) * 10.0 * p.Size * 2.0);   // r＝(所得÷物価÷人数)÷Y ⇒ 所得＝r×Y×人数×物価（Y＝10・物価 2 の呼び出しで）
            if (bigCurrentEarned) p.EarnedPerMonth = new MoneyPerMonth(1e9 * p.Size);   // 今の月の Earned（Run の冒頭で LastEarned へ写されゼロ化される）＝分子に使えば結果が変わる
        }
        w.Region.OpiumAddiction = 0.05;   // アヘンの必要量は中毒の在庫で立つ（所得で決まらない）＝需要が立った状態で〈掛けない〉ことを確かめる
        EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
        return (w.Region, w);
    }

    /// <summary>最終財の奢侈品（他の財の材料にならない・参照の需要が立つ）。必要量が人数だけに比例する財に限る（新聞は識字比例・ラジオも除く）。前提が崩れたら赤（空洞の主張を防ぐ）。</summary>
    private static int[] PureLuxuryGoods(Region region)
    {
        var used = new HashSet<int>();
        foreach (var g in Catalog.Goods) foreach (var inp in g.Inputs) used.Add(inp.GoodId);
        return region.Goods.Where(g => g.Tier == GoodTier.Luxury && g.Id != Catalog.OpiumId && g.Id != Catalog.NewspaperId && g.Id != Catalog.RadioId
            && !used.Contains(g.Id) && region.Markets[g.Id].LastDemand > 0).Select(g => g.Id).ToArray();
    }

    /// <summary>手の算術の期待倍率＝Σ(人数×f)÷Σ(人数)。f＝Pow(r, ε)：貴族 r 0.25・農民 r 4・他 r 1（ε＝0.5 なら 0.5・2・1）。</summary>
    private static double ExpectedMultiplier(Region region, double epsilon)
    {
        double s = 0, sf = 0;
        foreach (var p in region.Pops)
        {
            double r = p.ClassId == PopClass.Noble ? 0.25 : p.ClassId == PopClass.Peasant ? 4.0 : 1.0;
            s += p.Size; sf += p.Size * Math.Pow(r, epsilon);
        }
        return sf / s;
    }

    [Fact]
    public void 奢侈品の欲しい量は買い手の階級ごとのr乗_国単位の一律の倍率ではない_日用品と主食とアヘンは変わらない()
    {
        var (a, _) = OneMonth(0.0, 0.0);   // 基線（ε＝0）
        var (b, _) = OneMonth(0.5, 0.0);   // ε＝0.5・貴族 r＝0.25（×0.5）・農民 r＝4（×2）・他 r＝1（×1）
        var lux = PureLuxuryGoods(a);
        Assert.True(lux.Length >= 2, $"前提が崩れた：基線で需要の立つ最終財の奢侈品が {lux.Length} 本しか無い（主張が空洞）");
        double sizeNoble = a.Pops.Where(p => p.ClassId == PopClass.Noble).Sum(p => p.Size), sizePeasant = a.Pops.Where(p => p.ClassId == PopClass.Peasant).Sum(p => p.Size);
        Assert.True(sizeNoble > 0 && sizePeasant > 0, "前提が崩れた：貴族か農民の人数が 0（階級ごとの係数を試せない）");
        double expected = ExpectedMultiplier(a, 0.5);
        Assert.True(Math.Abs(expected - 1.0) > 0.01 && expected < 2.0 && expected > 0.5, $"前提が崩れた：期待倍率 {expected} が 1 に近い／一律の倍率（2 か 0.5）と区別できない");
        foreach (int gi in lux)
            Assert.Equal(expected * a.Markets[gi].LastDemand, b.Markets[gi].LastDemand, 9);   // 手の算術：Σ(人数×√r)÷Σ人数（予算は拘束しない）
        // 奢侈品以外（主食・日用品・役務・アヘン）は1ビットも変わらない（家事奉仕は切り替え 0 なので役務も同じ）。
        int unchanged = 0;
        foreach (var g in a.Goods)
        {
            if (g.Tier == GoodTier.Luxury && g.Id != Catalog.OpiumId) continue;
            Assert.Equal(a.Markets[g.Id].LastDemand, b.Markets[g.Id].LastDemand);
            if (g.Tier == GoodTier.Daily && a.Markets[g.Id].LastDemand > 0) unchanged++;
        }
        Assert.True(unchanged >= 3, $"前提が崩れた：需要の立つ日用品が {unchanged} 本（主張が空洞）");
        Assert.True(a.Markets[Catalog.OpiumId].LastDemand > 0, "前提が崩れた：アヘンの需要が立っていない（主張が空洞）");
        Assert.Equal(a.Markets[Catalog.OpiumId].LastDemand, b.Markets[Catalog.OpiumId].LastDemand);
    }

    [Fact]
    public void 家事奉仕は切り替えが立つときだけ同じ式に入る()
    {
        var (a, _) = OneMonth(0.5, 0.0);
        var (b, _) = OneMonth(0.5, 1.0);
        var (c, _) = OneMonth(0.0, 0.0);
        int svc = Catalog.DomesticServiceId;
        Assert.True(c.Markets[svc].LastDemand > 0, "前提が崩れた：家事奉仕の需要が立っていない（主張が空洞）");
        Assert.Equal(c.Markets[svc].LastDemand, OneMonth(0.0, 1.0).Region.Markets[svc].LastDemand);   // ε＝0 なら切り替えが立っても変わらない
        Assert.Equal(c.Markets[svc].LastDemand, a.Markets[svc].LastDemand);   // 切り替え 0 の家事奉仕は ε があっても基線と同じ
        double expected = ExpectedMultiplier(a, 0.5);
        Assert.Equal(expected * c.Markets[svc].LastDemand, b.Markets[svc].LastDemand, 9);
        // 他の役務（教育・医療）は切り替えが立っても変わらない。
        Assert.Equal(a.Markets[Catalog.EducationId].LastDemand, b.Markets[Catalog.EducationId].LastDemand);
        Assert.Equal(a.Markets[Catalog.HealthcareId].LastDemand, b.Markets[Catalog.HealthcareId].LastDemand);
    }

    [Fact]
    public void 世界の値が正でないとき_所得が正でない人は中立()
    {
        var (a, _) = OneMonth(0.0, 0.0);
        var lux = PureLuxuryGoods(a);
        Assert.True(lux.Length >= 2);
        // 世界の値が 0・負・NaN なら全ての POP が中立（欲しい量を NaN にも 0 にも負にもしない）
        foreach (double badY in new[] { 0.0, -3.0, double.NaN })
        {
            var (r, _) = OneMonth(0.5, 1.0, worldY: badY);
            foreach (int gi in lux) Assert.Equal(a.Markets[gi].LastDemand, r.Markets[gi].LastDemand);
        }
        // POP の所得が 0・負（資本家の Earned は粗利潤）なら、その POP だけ中立。農民の r を 0 と −5 にしても貴族 0.25 の係数 0.5 だけが残る。
        foreach (double badR in new[] { 0.0, -5.0 })
        {
            var (r, _) = OneMonth(0.5, 0.0, rOf: p => p.ClassId == PopClass.Noble ? 0.25 : p.ClassId == PopClass.Peasant ? badR : 1.0);
            double s = 0, sf = 0;
            foreach (var p in r.Pops) { s += p.Size; sf += p.Size * (p.ClassId == PopClass.Noble ? 0.5 : 1.0); }
            foreach (int gi in lux) Assert.Equal(sf / s * a.Markets[gi].LastDemand, r.Markets[gi].LastDemand, 9);
        }
    }

    [Fact]
    public void ε0では世界の値がどうあっても需要は変わらない()
    {
        var (a, _) = OneMonth(0.0, 1.0, worldY: 10.0);
        var (b, _) = OneMonth(0.0, 1.0, worldY: 0.001);
        foreach (var g in a.Goods) Assert.Equal(a.Markets[g.Id].LastDemand, b.Markets[g.Id].LastDemand);
    }

    [Fact]
    public void 分子は上書きの前の月の所得_今の月のEarnedや上書き後の欄ではない()
    {
        // 前の月の所得（LastEarnedPerMonth）で r を決め、今の月の Earned は無関係：Earned を大きく置いても結果が変わらない（分子と分母が同じ月・同じ欄）。
        var (a, _) = OneMonth(0.5, 0.0);
        var (b, _) = OneMonth(0.5, 0.0, bigCurrentEarned: true);
        var lux = PureLuxuryGoods(a);
        Assert.True(lux.Length >= 2);
        foreach (int gi in lux) Assert.Equal(a.Markets[gi].LastDemand, b.Markets[gi].LastDemand, 9);
    }

    [Fact]
    public void 計器の奢侈品の輸出額はLuxury階層の和でアヘンと他の階層を含まない_手の算術()
    {
        // 期待値は手で決める：Luxury 階層の（アヘンでない）財 2 つに 3 と 5・アヘンに 100・主食に 1000 を置く ⇒ 8。
        var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
        var lux = Catalog.Goods.Where(g => g.Tier == GoodTier.Luxury && g.Id != w.Coef.OpiumGoodId).Select(g => g.Id).ToArray();
        Assert.True(lux.Length >= 2, "前提が崩れた：アヘン以外の Luxury の財が 2 つ無い（主張が空洞）");
        int staple = Catalog.Goods.First(g => g.Tier == GoodTier.Staple).Id;
        var eg = new double[Catalog.Goods.Length];
        eg[lux[0]] = 3; eg[lux[1]] = 5; eg[w.Coef.OpiumGoodId] = 100; eg[staple] = 1000;
        w.ExportsByGood = eg;
        var cols = Instruments.Extras329(w);
        Assert.Equal("luxuryExportsPerClearing", cols[1].Name);
        Assert.Equal(8.0, double.Parse(cols[1].Value, System.Globalization.CultureInfo.InvariantCulture), 9);
        Assert.Equal("worldRealIncomePerCapitaPerMonth", cols[0].Name);
    }
}
