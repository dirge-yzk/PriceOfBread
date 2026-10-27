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
        => w.Regions.SelectMany(r => r.Producers).Where(FactoryEquipment.Eligible);

    [Fact]
    public void 旗は既定1で点火済み_旗0は旧の規則への戻し先()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.RecipeEquipment);          // 2026-10-23 点火（事前登録 追記 6）
        Assert.Equal(1.0, c.IndustrializationOff);
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
    public void 配り_配る合計は在庫に対する割合_個体へは利幅に比例_民間の合計は薄めて保つ()
    {
        var w = Make(12, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.SmoothedMargin = 0; }
        ps[0].Scale = 1.0; ps[1].Scale = 2.0; ps[2].Scale = 1.0; ps[3].Scale = 0.0;
        ps[0].SmoothedMargin = 1.0; ps[1].SmoothedMargin = 3.0; ps[2].SmoothedMargin = -2.0; ps[3].SmoothedMargin = 4.0;   // 3 つ目は赤字＝重み 0
        double[] before = ps.Select(p => p.Scale).ToArray();
        double g = 0.03, ind = 0.27;
        double given = FactoryEquipment.Distribute(r, w.Coef, g, ind, null);
        // 手計算：基準＝Σ Scale＝4（FactoryBuildScale 0.5 より大きい）・φ＝0.03÷(0.27＋0.03)＝0.1・配る合計＝0.4・重み＝1:3:0:4＝8 分の 1・3・0・4 → 配った後 1.05・2.15・1.0・0.2（合計 4.4）。
        // 民間の持分の合計を保つ（尺度を増やさない）ので全員を 4÷4.4 倍へ薄める → 0.954545…・1.954545…・0.909090…・0.181818…（合計 4）。
        Assert.Equal(0.4, given, 12);                                  // 戻り値＝配った合計（薄める前）
        double k = 4.0 / 4.4;
        Assert.Equal(1.05 * k, ps[0].Scale, 12);
        Assert.Equal(2.15 * k, ps[1].Scale, 12);
        Assert.Equal(1.0 * k, ps[2].Scale, 12);                        // 赤字の個体は受け取らず、薄められるだけ
        Assert.Equal(0.20 * k, ps[3].Scale, 12);
        Assert.Equal(before.Sum(), ps.Sum(p => p.Scale), 12);          // 尺度が増えない＝適格な個体の Scale の合計は購入の前後で同じ
        Assert.True(ps[3].Scale > before[3], "利幅の高い個体の取り分が増えない");
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
    public void 配り_発見の門が閉じた個体は受け取らない()
    {
        var w = Make(18, 1, 0).World;
        Region? reg = null; Producer? closed = null;
        foreach (var r in w.Regions)
        {
            closed = r.Producers.FirstOrDefault(p => FactoryEquipment.Eligible(p) && !(Discovery.Gate(r, p) > 0));
            if (closed is not null) { reg = r; break; }
        }
        Assert.NotNull(closed);   // 開幕に未発見の工場のレシピが無い盤面は想定外
        closed!.SmoothedMargin = 100.0; closed.Scale = 0;
        foreach (var p in reg!.Producers) if (FactoryEquipment.Eligible(p) && !ReferenceEquals(p, closed)) { p.Scale = 1.0; p.SmoothedMargin = 0.1; }
        FactoryEquipment.Distribute(reg, w.Coef, 0.5, 0.5, null);
        Assert.Equal(0.0, closed.Scale);   // 門が閉じた個体は（利幅が桁違いに高くても）設備を受け取らない
    }

    [Fact]
    public void 配り_国営の持分を持つ個体は薄めない_国営の持分を持たない個体だけが薄まり民間の合計は保たれる()
    {
        var w = Make(17, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.BuiltScale = 0; p.SmoothedMargin = 0; }
        ps[0].Scale = 3.0; ps[0].BuiltScale = 1.0; ps[0].SmoothedMargin = 0.5;   // 混ざった工場：国営 1・民間 2
        ps[1].Scale = 1.0; ps[1].BuiltScale = 0.0; ps[1].SmoothedMargin = 1.5;   // 民間 1
        ps[2].SmoothedMargin = -1; ps[3].SmoothedMargin = -1; ps[2].Bankrupt = true; ps[3].Bankrupt = true;
        // 手計算：Σ Scale＝4・φ＝0.1÷(0.4＋0.1)＝0.2・配る合計 4×0.2＝0.8・利幅 0.5:1.5＝1:3 → ps[0] へ 0.2・ps[1] へ 0.6。
        // 民間の持分の合計（前）P＝(3−1)＋1＝3。配った後：ps[0] の民間 2.2（混ざった工場）・ps[1] 1.6。国営の持分を持たない個体だけ k＝(3−2.2)÷1.6＝0.5 を掛ける → ps[1]＝0.8。
        double given = FactoryEquipment.Distribute(r, w.Coef, 0.1, 0.4, null);
        Assert.Equal(0.8, given, 12);
        Assert.Equal(1.0, ps[0].BuiltScale, 12);                        // 国営の持分は動かさない
        Assert.Equal(3.2, ps[0].Scale, 12);                             // 混ざった工場は自分への購入を受けるだけ（薄めない）＝国営の持分 1÷3.2 は自分への購入でだけ下がる
        Assert.Equal(0.8, ps[1].Scale, 12);
        Assert.Equal(4.0, ps.Sum(p => p.Scale), 12);                    // 尺度が増えない（合計は購入の前後で同じ）
        Assert.Equal(3.0, (ps[0].Scale - ps[0].BuiltScale) + ps[1].Scale, 12);   // 民間の持分の合計は保たれる
    }

    [Fact]
    public void 配り_他の個体の購入では混ざった工場の国営の持分の割合が動かない()
    {
        var w = Make(25, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.BuiltScale = 0; p.SmoothedMargin = 0; }
        ps[0].Scale = 3.0; ps[0].BuiltScale = 1.0; ps[0].SmoothedMargin = 0;      // 利幅 0＝受け取らない（重みは 0）
        ps[1].Scale = 1.0; ps[1].SmoothedMargin = 2.0;                            // 受け取る側
        ps[2].Bankrupt = true; ps[3].Bankrupt = true;
        double shareBefore = ps[0].BuiltScale / ps[0].Scale;
        FactoryEquipment.Distribute(r, w.Coef, 0.1, 0.4, null);
        Assert.Equal(shareBefore, ps[0].BuiltScale / ps[0].Scale, 12);            // 国営の持分は取引か自分への購入でしか動かない
        Assert.True(ps[1].Scale > 0);
    }

    [Fact]
    public void 配り_州に民間の設備が無ければ尺度が育つ_薄める先が無い()
    {
        var w = Make(26, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.BuiltScale = 0; p.SmoothedMargin = 0; }
        ps[0].SmoothedMargin = 1.0;
        double given = FactoryEquipment.Distribute(r, w.Coef, 0.1, 0.0, null);
        Assert.True(given > 0);
        Assert.Equal(given, ps[0].Scale, 12);   // 民間の設備が 0 の州は消さずに育つ（設備を持てなくなる行き止まりを作らない）
    }

    [Fact]
    public void 退場_門が閉じた個体の民間の持分は同じ州の薄める集合へ比例で戻る_国営の持分を持つ個体は動かない()
    {
        var w = Make(27, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.BuiltScale = 0; p.SmoothedMargin = 0; }
        ps[0].Scale = 3.0; ps[0].BuiltScale = 1.0;     // 混ざった工場（動かない）
        ps[1].Scale = 1.0; ps[2].Scale = 3.0;          // 国営の持分を持たない個体
        // 退場する個体：門が閉じた適格な個体（盤面から探して Scale を置く）
        var closed = r.Producers.FirstOrDefault(p => FactoryEquipment.Eligible(p) && !(Discovery.Gate(r, p) > 0));
        Assert.NotNull(closed);
        closed!.Scale = 2.0; closed.BuiltScale = 0;
        double pdBefore = (ps[0].Scale - ps[0].BuiltScale) + ps[1].Scale + ps[2].Scale;   // 2＋1＋3＝6
        FactoryEquipment.Retire(r, closed, null);
        // 目標 6＋2＝8・混ざった個体の民間 2・国営の持分を持たない個体の Scale の和 4 → k＝(8−2)÷4＝1.5
        Assert.Equal(0.0, closed.Scale, 12);
        Assert.Equal(3.0, ps[0].Scale, 12); Assert.Equal(1.0, ps[0].BuiltScale, 12);
        Assert.Equal(1.5, ps[1].Scale, 12); Assert.Equal(4.5, ps[2].Scale, 12);
        Assert.Equal(pdBefore + 2.0, (ps[0].Scale - ps[0].BuiltScale) + ps[1].Scale + ps[2].Scale, 12);
        Assert.Equal(0.0, ps[3].Scale, 12);
    }

    [Fact]
    public void 退場_戻す先が無ければ設備は消える_国営の持分はScaleから引かない()
    {
        var w = Make(28, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.BuiltScale = 0; p.SmoothedMargin = 0; }
        var closed = r.Producers.FirstOrDefault(p => FactoryEquipment.Eligible(p) && !(Discovery.Gate(r, p) > 0));
        Assert.NotNull(closed);
        closed!.Scale = 2.0; closed.BuiltScale = 0.5;   // 民間 1.5・国営 0.5
        var cost = new long[CostLedger.Count];
        FactoryEquipment.Retire(r, closed, cost);
        Assert.Equal(0.0, closed.Scale, 12);
        Assert.All(ps, p => Assert.Equal(0.0, p.Scale, 12));    // 戻す先（民間の設備を持つ個体）が無い＝消える（尺度を増やさない）
        Assert.Equal(2L * r.Producers.Count, cost[CostLedger.RecipeEquipmentScans]);
    }

    /// <summary>★(500) 合流で足した番人（反証役 eq〔bv〕低#5）：薄めきれない経路（混ざった個体の民間の持分だけで目標を超えた＝k の分子が負）は国営の持分を持たない個体に触らない（k＝1）。
    /// 期待は盤面に自分で置いた数からの手計算（基準＝Σ Scale＝3.5・φ＝1÷(0＋1)＝1・配る合計 3.5 を利幅の正の 1 個体〔混ざった個体〕へ）。</summary>
    [Fact]
    public void 配り_薄めきれないときは国営の持分を持たない個体に触らない()
    {
        var w = Make(12, 1, 0).World;
        var (r, ps) = FourProducers(w);
        foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p)) { p.Scale = 0; p.BuiltScale = 0; p.SmoothedMargin = 0; }
        ps[0].Scale = 3.0; ps[0].BuiltScale = 1.0; ps[0].SmoothedMargin = 1.0;   // 混ざった個体：民間 2.0・国営 1.0
        ps[1].Scale = 0.5;                                                       // 国営の持分を持たない個体：利幅 0＝配りの重み 0
        double given = FactoryEquipment.Distribute(r, w.Coef, 1.0, 0.0, null);
        // 手計算：配った後の混ざった個体 6.5（民間 5.5）＞ 目標の民間の合計 2.5（2.0＋0.5）＝k の分子 2.5−5.5＝−3＜0＝薄めきれない＝触らない。
        Assert.Equal(3.5, given, 12);
        Assert.Equal(6.5, ps[0].Scale, 12);
        Assert.Equal(1.0, ps[0].BuiltScale, 12);
        Assert.Equal(0.5, ps[1].Scale, 12);      // k を当てる変異では負か 0 になる
        Assert.Equal(0.0, ps[2].Scale, 12);
        Assert.Equal(0.0, ps[3].Scale, 12);
    }

    private static double PrivateOfSet(Region r)
        => r.Producers.Where(p => FactoryEquipment.InSet(r, p)).Sum(p => Math.Max(0.0, p.Scale - p.BuiltScale));

    [Fact]
    public void 配りと退場は民間の持分の合計を保つ_手計算でない盤面の性質_国営の持分は動かない()
    {
        // 決定的な擬似乱数で盤面を 40 通り作る（混ざった工場・国営だけの工場・倒産・利幅の符号を混ぜる）。期待は盤面に置いた数の和（実装と別の出所）。
        var w = Make(61, 1, 0).World;
        var (r, ps) = FourProducers(w);
        var elig = r.Producers.Where(FactoryEquipment.Eligible).ToList();
        ulong x = 0x9E3779B97F4A7C15UL;
        double U() { x ^= x << 13; x ^= x >> 7; x ^= x << 17; return (x >> 11) / (double)(1UL << 53); }
        for (int t = 0; t < 40; t++)
        {
            foreach (var p in elig)
            {
                bool open = Discovery.Gate(r, p) > 0;
                double u = U();
                p.Scale = open ? U() * 4.0 : 0.0;
                p.BuiltScale = u < 0.25 && open ? Math.Min(p.Scale, U()) : 0.0;
                p.Bankrupt = U() < 0.2;
                p.SmoothedMargin = (U() - 0.4) * 3.0;
            }
            double pd = PrivateOfSet(r), tot = elig.Sum(p => p.Scale);
            var built = elig.Select(p => p.BuiltScale).ToArray();
            double given = FactoryEquipment.Distribute(r, w.Coef, 0.02 + U() * 0.2, 0.1 + U() * 2, null);
            if (pd > 1e-9 && given > 0 && elig.Where(p => FactoryEquipment.InSet(r, p) && !(p.BuiltScale > 1e-9) && p.Scale > 1e-9).Any())
                Assert.True(Math.Abs(PrivateOfSet(r) - pd) <= 1e-9 * Math.Max(1, pd) || elig.Where(p => FactoryEquipment.InSet(r, p) && p.BuiltScale > 1e-9).Sum(p => Math.Max(0.0, p.Scale - p.BuiltScale)) > pd,
                    $"民間の持分の合計が動いた t={t} {pd}→{PrivateOfSet(r)}");   // 薄めきれない（混ざった工場だけで目標を超えた）ときだけ例外
            Assert.Equal(built, elig.Select(p => p.BuiltScale).ToArray());   // 配りは国営の持分に触れない
            Assert.True(elig.All(p => p.Scale >= 0 && double.IsFinite(p.Scale)));
            _ = tot;
        }
    }

    [Fact]
    public void 動いている世界で門が閉じた個体の設備は同じ州へ戻る_民間の持分の合計は退場で動かない()
    {
        var sim = Make(62, 1, 0);
        var w = sim.World;
        w.Coef.IndCapitalCost = 1e9; w.Coef.DevelopmentMemory = 0; w.Coef.DepressionScrapRate = 0;   // 購入と廃棄を止める
        sim.Run(1);
        Region? reg = null; Producer? closed = null;
        foreach (var r in w.Regions)
        {
            closed = r.Producers.FirstOrDefault(p => FactoryEquipment.Eligible(p) && !(Discovery.Gate(r, p) > 0));
            if (closed is not null && PrivateOfSet(r) > 1.0) { reg = r; break; }
            closed = null;
        }
        Assert.NotNull(closed);
        closed!.Scale = 2.5; closed.BuiltScale = 0;       // 退場すべき設備を置く
        double pd = PrivateOfSet(reg!);
        for (int d = 0; d < 31; d++) sim.StepDay();
        Assert.Equal(0.0, closed.Scale, 12);                                   // 作れない工場は設備を持たない
        Assert.Equal(pd + 2.5, PrivateOfSet(reg!), 9);                         // 消えた取り分は同じ州の他の個体へ（購入が無く廃棄も完成も無い月）
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
        Assert.Equal(3L * r.Producers.Count, cost[CostLedger.RecipeEquipmentScans]);   // 合計・配り・薄めの 3 周
        Assert.Equal(3L * r.Producers.Count, cost.Sum());   // 他の行には足さない
    }

    [Fact]
    public void 国営の休止中の設備の廃棄は民間の持分を動かさない()
    {
        var w = Make(16, 1, 0).World;
        var (r, ps) = FourProducers(w);
        ps[0].Scale = 2.0; ps[0].BuiltScale = 0.5;      // 民間 1.5・国営 0.5
        var rgo = r.Producers.First(p => p.IsRgo); double rgoScale = rgo.Scale, rgoBuilt = rgo.BuiltScale;
        double before = ps[0].BuiltScale;
        ps[0].BuiltScale *= 0.8;                        // 恐慌の廃棄（Simulation が掛ける）を模した
        FactoryEquipment.Dismantle(ps[0], before);
        Assert.Equal(1.5, ps[0].Scale - ps[0].BuiltScale, 12);          // 廃棄でも民間の持分は動かない
        Assert.Equal(0.4, ps[0].BuiltScale, 12);
        Assert.Equal(1.9, ps[0].Scale, 12);                              // 廃棄された国営の分だけ Scale が減る
        Assert.Equal(rgoScale, rgo.Scale); Assert.Equal(rgoBuilt, rgo.BuiltScale);   // 産地には触れない
        // BuiltScale が増えた（国営の完成ではない異常）場合は Scale を引かない
        double b2 = ps[1].BuiltScale; double s2 = ps[1].Scale;
        FactoryEquipment.Dismantle(ps[1], b2 - 1.0);
        Assert.Equal(s2, ps[1].Scale, 12);
    }

    [Fact]
    public void 旗1で購入も完成も廃棄も無ければ設備は動かない_自由な伸びは働かない()
    {
        var sim = Make(21, 1, 0);
        var w = sim.World;
        w.Coef.IndCapitalCost = 1e9;           // 私的な建設の止める信号（ハードルが総収益を超える）＝購入が起きない
        w.Coef.DevelopmentMemory = 0;          // 減価の係数を IndDepreciation そのものに（期待を自分で作れる）
        w.Coef.DepressionScrapRate = 0;        // 恐慌の廃棄を止める
        sim.Run(1);                            // 最初の月の門の整理（未発見の個体の Scale=0）を済ませる
        var start = new Dictionary<Producer, double>();
        var owner = new Dictionary<Producer, Region>();
        var built0 = new Dictionary<Producer, double>();
        foreach (var r in w.Regions)
            foreach (var p in r.Producers) if (FactoryEquipment.Eligible(p) && p.Scale > 0.01) { start[p] = p.Scale; owner[p] = r; built0[p] = p.BuiltScale; }
        Assert.True(start.Count > 20, $"設備のある適格な個体が少ない {start.Count}");
        int quarters = 4;
        sim.Run(quarters);
        int checkedN = 0;
        foreach (var (p, s0) in start)
        {
            if (!(Discovery.Gate(owner[p], p) > 0)) continue;        // 門の閉鎖（退場）の個体だけ対象外（Scale＝0 は設備が無い）。倒産で消えた個体は対象に残す＝倒産の遷移が Scale を消せば赤
            Assert.True(p.Scale > 0, $"門が開いているのに設備が消えた（倒産の遷移が Scale を 0 にした？） {p.GoodId} {s0}→{p.Scale} 倒産={p.Bankrupt}");
            Assert.True(p.Scale <= s0 * (1 + 1e-12), $"購入が無いのに設備が増えた {p.GoodId} {s0}→{p.Scale}");
            double b0 = built0[p];
            // 購入も国営の完成も廃棄も無ければ設備は一切動かない（時間の経過では民間も国営も摩耗しない）＝国営の持分 BuiltScale÷Scale も動かない。倒産しても Scale を消さない。
            Assert.Equal(s0, p.Scale, 9);
            Assert.Equal(b0, p.BuiltScale, 9);
            if (s0 > 1e-9) Assert.Equal(b0 / s0, p.BuiltScale / p.Scale, 9);
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
        p.Scale = 1.5; p.BuiltScale = 0; p.Bankrupt = true;
        sim.Run(1);
        Assert.True(p.Scale > 0, "倒産で設備が消えた（旧の Scale=0 が旗 1 でも走っている）");
        Assert.Equal(1.5, p.Scale, 9);   // 再参入しても据え置き（FactoryReentryScale 0.3 に置き換わらない）・設備は時間の経過では動かない
    }

    [Fact]
    public void 倍率の削除_旗で能力の倍率と賃金の天井と独占の解体が工業化度を読まない()
    {
        var w = Make(31, 0, 1).World;
        var r = w.Regions[0];
        r.Industrialization = 3.0;
        Assert.Equal(w.Coef.WageClampMax, MarketFlow.EffWageMax(r, w.Coef), 12);   // 固定の天井だけ（★(605) 工業化度の項は物理削除＝旗 0 の旧の式の枝は無い）
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
    public void 倒産の遷移で設備が消えない_深い赤字の個体は倒産しても_Scale_を保つ()
    {
        var sim = Make(24, 1, 0);
        var w = sim.World;
        w.Coef.IndCapitalCost = 1e9; w.Coef.DevelopmentMemory = 0; w.Coef.DepressionScrapRate = 0;
        sim.Run(1);                            // 最初の月の門の整理（退場する個体の設備を戻す）を済ませる＝この試験の盤面を動かさない
        var (r, ps) = FourProducers(w);
        var p = ps[0];
        p.BuiltScale = 0; p.Scale = 2.0; p.SmoothedMargin = -100.0; p.Bankrupt = false;
        for (int d = 0; d < 31; d++) sim.StepDay();
        Assert.True(p.Bankrupt, "深い赤字なのに倒産の遷移が起きなかった（試験の前提が崩れた）");
        Assert.True(Math.Abs(p.Scale - 2.0) < 1e-9, $"倒産で設備が消えた／置き換わった Scale={p.Scale}（旧の Scale=0 や再参入の 0.3 が旗 1 でも走っている）");
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
    [InlineData(@"\.Scale\s*(\+=|-=|\*=|/=|=(?![=>]))", 14)]   // Producer.Scale の書き手（適格な個体の自由な伸びは旗 RecipeEquipment が止める。設備の規則の書き手 FactoryEquipment〔配り・薄め・退場・廃棄〕・World の完成を含む。(495) 後半＝退場の Retire が 1 行増えた〔13→14〕。）
    public void ラチェット_倍率と操業規模と技術水準の側の書き手は増えない(string pattern, int ceiling)
    {
        int n = CountLines(pattern);
        Assert.True(n <= ceiling, $"書き手・参照が増えた {n} > {ceiling}（増やすなら基準値の更新を宣言する）: {pattern}");
    }
}
