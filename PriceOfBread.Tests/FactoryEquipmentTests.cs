using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(495) レシピごとの設備（旗 <c>RecipeEquipment</c>・<c>IndustrializationOff</c>）の番人＝機構と不変条件。事前登録 measurements/eq495_20261023_prereg.txt の C1（保存）・C2（買わないと増えない）・V1（減るのは減価だけ）・E1（倍率が読まれない）。
/// 期待は手計算のリテラルか、実装と別の出所（盤面に自分で置いた数）から作る。旗は既定 0（倒さない）。史実の桁は固定しない。</summary>
public class FactoryEquipmentTests
{
    private static Scenario Fr(ulong s) => new()
    {
        Seed = s, StartYear = 1789, Kind = ScenarioKind.France, PoliticsEnabled = true, InitialPolity = Polity.Absolutist,
        FiscalEnabled = true, TaxRate = 0.10, InitialPollTax = 1.0, InitialTreasury = 1_000_000, BaseCoercion = 400,
        EducationLaw = EducationLaw.Religious, ExternalPressure = 0.1,
    };

    private static Simulation Make(ulong seed, double recipeEquipment, double indOff)
    {
        var sim = new Simulation(Fr(seed), new NullAgent());
        sim.World.Coef.RecipeEquipment = recipeEquipment;
        sim.World.Coef.IndustrializationOff = indOff;
        return sim;
    }

    private static IEnumerable<Producer> Eligibles(World w)
        => w.Regions.Where(r => r.State == RegionState.Active).SelectMany(r => r.Producers).Where(FactoryEquipment.Eligible);

    [Fact]
    public void 旗は既定0で倒さない()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.RecipeEquipment);
        Assert.Equal(0.0, c.IndustrializationOff);
    }

    [Fact]
    public void 設備を持つ生産者は工場だけ_産地と役務と職人は持たない()
    {
        var sim = Make(11, 0, 0);
        var w = sim.World;
        int fac = 0, rgo = 0, svc = 0, craft = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                bool e = FactoryEquipment.Eligible(p);
                if (p.IsRgo) { Assert.False(e); rgo++; }
                else if (Catalog.Goods[p.GoodId].Tier == GoodTier.Service) { Assert.False(e); svc++; }
                else if (p.RecipeName == "職人") { Assert.False(e); craft++; }
                else if (p.BuysLabor) { Assert.True(e); fac++; }
            }
        Assert.True(fac > 20 && rgo > 20 && svc > 3 && craft > 0, $"盤面の組成が想定と違う fac={fac} rgo={rgo} svc={svc} craft={craft}");
    }

    /// <summary>州の中の適格な個体を 4 つ選び、盤面に自分で置いた数（Scale・利幅・倒産・門）で配りを手計算する。</summary>
    private static (Region r, List<Producer> ps) FourProducers(World w)
    {
        foreach (var r in w.Regions)
        {
            var cand = r.Producers.Where(p => FactoryEquipment.Eligible(p) && Discovery.Gate(r, p) > 0 && !p.Bankrupt).Take(4).ToList();
            if (cand.Count == 4) return (r, cand);
        }
        throw new Xunit.Sdk.XunitException("門が開いた適格な個体が 4 つ揃う州が無い");
    }

    [Fact]
    public void 配り_増える合計は在庫に対する割合_個体へは利幅に比例_保存()
    {
        var w = Make(12, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.SmoothedMargin = 0; }
        ps[0].Scale = 1.0; ps[1].Scale = 2.0; ps[2].Scale = 1.0; ps[3].Scale = 0.0;
        ps[0].SmoothedMargin = 1.0; ps[1].SmoothedMargin = 3.0; ps[2].SmoothedMargin = -2.0; ps[3].SmoothedMargin = 4.0;   // 3 つ目は赤字＝重み 0
        double[] before = ps.Select(p => p.Scale).ToArray();
        double g = 0.03, ind = 0.27;
        double given = FactoryEquipment.Distribute(r, w.Coef, g, ind, null);
        // 手計算：基準＝Σ Scale＝4（FactoryBuildScale 0.5 より大きい）・φ＝0.03÷(0.27＋0.03)＝0.1・増える合計＝0.4・重み＝1:3:0:4＝8 分の 1・3・0・4
        Assert.Equal(0.4, given, 12);
        Assert.Equal(0.4 * 1.0 / 8.0, ps[0].Scale - before[0], 12);
        Assert.Equal(0.4 * 3.0 / 8.0, ps[1].Scale - before[1], 12);
        Assert.Equal(0.0, ps[2].Scale - before[2], 12);
        Assert.Equal(0.4 * 4.0 / 8.0, ps[3].Scale - before[3], 12);
        Assert.Equal(given, ps.Sum(p => p.Scale) - before.Sum(), 12);   // 保存：配った合計＝個体の増えの和
    }

    [Fact]
    public void 配り_倒産と未発見の個体は受け取らない_全て赤字なら等分_基準は一棟の下限()
    {
        var w = Make(13, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.SmoothedMargin = 0; }
        ps[0].SmoothedMargin = -1; ps[1].SmoothedMargin = -1; ps[2].SmoothedMargin = -1; ps[3].SmoothedMargin = 5.0;
        ps[3].Bankrupt = true;   // 倒産中は受け取らない
        // 適格な個体の Scale の合計が 0 → 基準は FactoryBuildScale（0.5）。φ＝1.0÷(0＋1.0)＝1 → 増える合計 0.5。
        // 受け取れる個体は ps[0..2] 以外にも門が開いた他の適格な個体があるので、受け取った個体の数 n で等分された値を確かめる。
        double given = FactoryEquipment.Distribute(r, w.Coef, 1.0, 0.0, null);
        Assert.Equal(w.Coef.FactoryBuildScale, given, 12);
        Assert.Equal(0.0, ps[3].Scale, 12);
        var got = r.Producers.Where(p => FactoryEquipment.Eligible(p) && p.Scale > 0).ToList();
        Assert.True(got.Count >= 3);
        Assert.All(got, p => Assert.Equal(given / got.Count, p.Scale, 12));   // 全員が赤字（重み 0）＝等分
        Assert.DoesNotContain(got, p => p.Bankrupt || !(Discovery.Gate(r, p) > 0));
    }

    [Fact]
    public void 配り_点が0やNaNなら配らない_配り先が無ければ0()
    {
        var w = Make(14, 1, 0).World;
        var (r, ps) = FourProducers(w);
        double sum0 = Eligibles(w).Sum(p => p.Scale);
        Assert.Equal(0.0, FactoryEquipment.Distribute(r, w.Coef, 0.0, 1.0, null));
        Assert.Equal(0.0, FactoryEquipment.Distribute(r, w.Coef, double.NaN, 1.0, null));
        Assert.Equal(0.0, FactoryEquipment.Distribute(r, w.Coef, -1.0, 1.0, null));
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) p.Bankrupt = true;   // 配り先が無い
        Assert.Equal(0.0, FactoryEquipment.Distribute(r, w.Coef, 0.5, 1.0, null));
        Assert.Equal(sum0, Eligibles(w).Sum(p => p.Scale), 12);
    }

    [Fact]
    public void 配りは計算量の台帳に州の生産者の数だけ足す()
    {
        var w = Make(15, 1, 0).World;
        var (r, _) = FourProducers(w);
        var cost = new long[CostLedger.Count];
        FactoryEquipment.Distribute(r, w.Coef, 0.1, 0.5, cost);
        Assert.Equal(r.Producers.Count, cost[CostLedger.RecipeEquipmentScans]);
        Assert.Equal(r.Producers.Count, cost.Sum());   // 他の行には足さない
    }

    [Fact]
    public void 減価と廃棄は_Scale_と_BuiltScale_に同じ係数を掛け持分を動かさない()
    {
        var w = Make(16, 1, 0).World;
        var (r, ps) = FourProducers(w);
        ps[0].Scale = 2.0; ps[0].BuiltScale = 0.5;
        ps[1].Scale = 1.0; ps[1].BuiltScale = 1.0;
        var rgo = r.Producers.First(p => p.IsRgo); double rgoScale = rgo.Scale, rgoBuilt = rgo.BuiltScale;
        FactoryEquipment.Depreciate(r, 0.01);
        Assert.Equal(1.98, ps[0].Scale, 12); Assert.Equal(0.495, ps[0].BuiltScale, 12);
        Assert.Equal(0.99, ps[1].Scale, 12); Assert.Equal(0.99, ps[1].BuiltScale, 12);
        Assert.Equal(0.25, ps[0].BuiltScale / ps[0].Scale, 12);   // 持分は動かない
        FactoryEquipment.Scrap(r, 0.9);
        Assert.Equal(1.782, ps[0].Scale, 12); Assert.Equal(0.4455, ps[0].BuiltScale, 12);
        Assert.Equal(1.0, ps[1].BuiltScale / ps[1].Scale, 12);
        Assert.Equal(rgoScale, rgo.Scale); Assert.Equal(rgoBuilt, rgo.BuiltScale);   // 産地には触れない
    }

    [Fact]
    public void 旗1で購入が無ければ設備は減価でしか減らず増えない_自由な伸びは働かない()
    {
        var sim = Make(21, 1, 0);
        var w = sim.World;
        w.Coef.IndCapitalCost = 1e9;           // 私的な建設の止める信号（ハードルが総収益を超える）＝購入が起きない
        w.Coef.DevelopmentMemory = 0;          // 減価の係数を IndDepreciation そのものに（期待を自分で作れる）
        w.Coef.DepressionScrapRate = 0;        // 恐慌の廃棄を止める
        sim.Run(1);                            // 最初の月の門の整理（未発見の個体の Scale=0）を済ませる
        var start = new Dictionary<Producer, double>();
        foreach (var p in Eligibles(w)) if (p.Scale > 0.01) start[p] = p.Scale;
        Assert.True(start.Count > 20, $"設備のある適格な個体が少ない {start.Count}");
        int quarters = 4;
        sim.Run(quarters);
        double keep = Math.Pow(1.0 - w.Coef.IndDepreciation, 3 * quarters);
        int checkedN = 0;
        foreach (var (p, s0) in start)
        {
            if (p.Scale == 0) continue;        // 門の閉鎖（退場）で消えた個体は対象外
            Assert.True(p.Scale <= s0 * (1 + 1e-12), $"購入が無いのに設備が増えた {p.GoodId} {s0}→{p.Scale}");
            Assert.Equal(s0 * keep, p.Scale, 9);   // 減るのは減価だけ（倒産しても Scale を消さない）
            checkedN++;
        }
        Assert.True(checkedN > 15, $"検証できた個体が少ない {checkedN}");
    }

    [Fact]
    public void 旗0では設備の規則は走らず_自由な伸びが残る()
    {
        var sim = Make(22, 0, 0);
        var w = sim.World;
        w.Coef.IndCapitalCost = 1e9;
        sim.Run(1);
        var start = Eligibles(w).Where(p => p.Scale > 0.01).ToDictionary(p => p, p => p.Scale);
        sim.Run(4);
        int grew = start.Count(kv => kv.Key.Scale > kv.Value * 1.02);
        Assert.True(grew > 0, "旗 0 で操業規模が利幅へ伸びる経路が死んでいる（旧の規則の対照）");
    }

    [Fact]
    public void 倒産しても設備を消さず労働配分の重みは0_再参入でも据え置き()
    {
        var sim = Make(23, 1, 0);
        var w = sim.World;
        w.Coef.IndCapitalCost = 1e9; w.Coef.DevelopmentMemory = 0; w.Coef.DepressionScrapRate = 0;
        sim.Run(1);
        var (r, ps) = FourProducers(w);
        var p = ps[0];
        p.Scale = 1.5; p.Bankrupt = true;
        sim.Run(1);
        Assert.True(p.Scale > 0, "倒産で設備が消えた（旧の Scale=0 が旗 1 でも走っている）");
        Assert.Equal(1.5 * Math.Pow(1.0 - w.Coef.IndDepreciation, 3) , p.Scale, 6);   // 再参入しても据え置き（FactoryReentryScale 0.3 に置き換わらない）
    }

    [Fact]
    public void 倍率の削除_旗で能力の倍率と賃金の天井と独占の解体が工業化度を読まない()
    {
        var w = Make(31, 0, 1).World;
        var r = w.Regions[0];
        r.Industrialization = 3.0;
        Assert.Equal(w.Coef.WageClampMax, MarketFlow.EffWageMax(r, w.Coef), 12);   // 旗 1＝固定の天井だけ
        w.Coef.IndustrializationOff = 0;
        Assert.Equal(w.Coef.WageClampMax * (1 + w.Coef.WageClampIndScale * 3.0), MarketFlow.EffWageMax(r, w.Coef), 12);   // 旗 0＝旧の式
        w.Coef.IndustrializationOff = 1;
        double prem = 0.15;
        r.Industrialization = 0; double m0 = EconomyStep.MonopolyDegree(r, w.Coef, prem);
        r.Industrialization = 10; double m10 = EconomyStep.MonopolyDegree(r, w.Coef, prem);
        Assert.Equal(m0, m10, 12);   // 工業化度で独占が解体しない
    }

    [Fact]
    public void 倍率の削除_走らせてもヴィンテージの3つは0のまま_機械化度は0()
    {
        var sim = Make(32, 0, 1);
        var w = sim.World;
        sim.Run(4);
        foreach (var r in w.Regions)
        {
            Assert.Equal(0.0, r.OldVintageCapital, 12);
            Assert.Equal(0.0, r.StrandedVintage, 12);
            Assert.Equal(0.0, r.VintageTechLevel, 12);
            Assert.Equal(0.0, r.Mechanization, 12);
        }
        // 対照：旗 0 ではヴィンテージが貯まる（経年の移行が live）
        var s0 = Make(32, 0, 0); s0.Run(4);
        Assert.True(s0.World.Regions.Any(r => r.OldVintageCapital > 0), "旗 0 のヴィンテージの移行が死んでいる（対照）");
    }

    [Fact]
    public void 国家の工場の完成は適格な個体の設備にも足される_旗0は床が引き上げる()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var sim = Make(41, flag, 0);
            var w = sim.World;
            sim.Run(1);
            var (r, ps) = FourProducers(w);
            var p = ps[0];
            p.Scale = 1.0; p.BuiltScale = 0; p.ConstructionLeft = 1e-12; p.ConstructionPayPerDay = 0;
            w.StepConstruction(1);
            Assert.Equal(0.0, p.ConstructionLeft);
            Assert.Equal(w.Coef.FactoryBuildScale, p.BuiltScale, 12);
            if (flag > 0) Assert.Equal(1.0 + w.Coef.FactoryBuildScale, p.Scale, 12);   // 国営の設備も買った分だけ
            else Assert.Equal(1.0, p.Scale, 12);                                       // 旗 0＝完成では Scale を足さない（床が引き上げる）
        }
    }

    [Fact]
    public void 倍率の削除_旗で人力の労働の能力が工業化度の倍率を受けない()
    {
        // 人力（労働力の既定のレシピ）の能力＝担い手の頭数×生産性×boost で boost＝indMult×門（門は 1）＝価格に依らない。同じ盤面（工業化度 2.0 を置く）で旗 0 と旗 1 を 1 か月走らせ、比が indMult の式の値になる。
        double Cap(double indOff, out double ind)
        {
            var sim = Make(33, 0, indOff);
            var w = sim.World;
            w.Coef.IndCapitalCost = 1e9; w.Coef.DevelopmentMemory = 0; w.Coef.VintageMigrateRate = 0; w.Coef.DepressionScrapRate = 0;
            var r = w.Regions[0];
            r.Industrialization = 2.0;
            for (int d = 0; d < 31; d++) sim.StepDay();
            ind = r.Industrialization;
            return r.Producers.First(p => p.GoodId == Catalog.LaborId && p.RecipeName.Length == 0).Capacity;
        }
        double cOn = Cap(1, out double indOn), cOff = Cap(0, out double indOff);
        Assert.Equal(indOn, indOff, 12);   // 工業化度の動きは同じ（購入が無く減価だけ）
        double expected = 1.0 + new Coefficients().IndProductivityGain * indOff / (1.0 + indOff / new Coefficients().IndProductivityScale);
        Assert.True(expected > 1.5, $"盤面の想定が外れた expected={expected}");
        Assert.Equal(expected, cOff / cOn, 9);
    }

    [Fact]
    public void 倒産した適格な個体は労働配分の重みを持たない_兄弟の能力が倒産の設備の大きさに依らない()
    {
        double SiblingCapacity(double rivalScale)
        {
            var sim = Make(34, 1, 0);
            var w = sim.World;
            w.Coef.IndCapitalCost = 1e9;
            var (r, ps) = FourProducers(w);
            var rival = ps[0]; var sib = ps[1];
            Assert.Equal(rival.LaborPopIndex, sib.LaborPopIndex);
            rival.Bankrupt = true; rival.Scale = rivalScale; sib.Scale = 1.0;
            for (int d = 0; d < 31; d++) sim.StepDay();
            return sib.Capacity;
        }
        double a = SiblingCapacity(100.0), b = SiblingCapacity(0.0);
        Assert.True(a > 0);
        Assert.Equal(b, a, 9);   // 倒産の個体が大きな設備を持っていても、労働の取り分を取らない
    }

    // ---- T0 ラチェット（事前登録 追記 1 (5)）：この線が減らす物の書き手・参照の数。増えたら赤＝増やすなら基準値の更新を宣言する。焼き込み（旗を既定にして旧の腕を削除する合流）で 0 へ下げる。
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    private static int CountLines(string pattern)
    {
        var rx = new Regex(pattern);
        int n = 0;
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
            foreach (var line in File.ReadLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                var code = c >= 0 ? line[..c] : line;
                if (rx.IsMatch(code)) n++;
            }
        return n;
    }

    [Theory]
    [InlineData(@"\bindMult\b", 3)]   // 能力の倍率 indMult（宣言 1・使う式 2）＝旗 IndustrializationOff が 1.0 に落とす
    [InlineData(@"\.(OldVintageCapital|StrandedVintage|VintageTechLevel)\s*(\+=|-=|\*=|/=|=(?![=>]))", 8)]   // ヴィンテージの 3 つの書き手（旗 IndustrializationOff が移行と座礁を止める）
    [InlineData(@"\.Scale\s*(\+=|-=|\*=|/=|=(?![=>]))", 13)]   // Producer.Scale の書き手（適格な個体の自由な伸びは旗 RecipeEquipment が止める。設備の規則の書き手 FactoryEquipment・World の完成を含む）
    public void ラチェット_倍率と操業規模と技術水準の側の書き手は増えない(string pattern, int ceiling)
    {
        int n = CountLines(pattern);
        Assert.True(n <= ceiling, $"書き手・参照が増えた {n} > {ceiling}（増やすなら基準値の更新を宣言する）: {pattern}");
    }
}
