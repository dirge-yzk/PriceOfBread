using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(342) 段2：財の性質〈習慣性〉（<see cref="Habit"/>・<see cref="HabitProfile"/>）の番人。アヘンの中毒の更新は旧の式（<c>OpiumAddictionGrowth</c> ほかの直読み）と同じ値で、他の財は 0（効かない）。
/// 期待値はコードの式を呼ばず、リテラルの算術で書く（0.1・0.02・0.2・1.0・2.0 は係数の既定値＝別の出所）。
/// 突然変異（一時的に壊して赤を確かめる・結果は実装ログ (342)）：①<c>NextStock</c> の減衰を 1.1 倍 ②<c>NextStock</c> の上限を外す ③<c>LaborFactor</c> の床を 0.4 に ④<c>Of</c> が全ての財に行を返す
/// ⑤<c>EconomyStep</c> の更新を <c>Active</c>（成長&gt;0）で守る ⑥<c>LaborFactor</c> の主フラグを外す ⑦<c>EconomyStep</c> の <c>adults *= </c> の行を消す ⑧所得弾力の除外を <c>Active</c> に。
/// ★(344) 反証役 op〔v〕：計器の全体の口（<c>Instruments.Configure</c>）を立てる試験があるので、並列を外す集まり〈Instruments〉に入れた（<c>InstrumentsTests</c>・<c>LuxuryRatioDiagTests</c> と同じ）。</summary>
[Collection("Instruments")]
public class HabitTests
{
    [Fact]
    public void アヘンの行は今の係数の値_他の財は全て0で効かない()
    {
        var c = new Coefficients();
        var h = Habit.Of(c, Catalog.OpiumId);
        Assert.True(h.Forms); Assert.True(h.Active);
        Assert.Equal(0.1, h.Growth); Assert.Equal(0.02, h.Decay); Assert.Equal(0.2, h.Cap); Assert.Equal(1.0, h.LaborPenalty); Assert.Equal(2.0, h.MiseryScale);
        foreach (var g in Catalog.Goods)
        {
            if (g.Id == Catalog.OpiumId) continue;
            var o = Habit.Of(c, g.Id);
            Assert.False(o.Forms); Assert.False(o.Active);
            Assert.Equal(0.0, o.Growth); Assert.Equal(0.0, o.Decay); Assert.Equal(0.0, o.Cap); Assert.Equal(0.0, o.LaborPenalty); Assert.Equal(0.0, o.MiseryScale);
        }
        // 係数を変えれば行が追う（値の出所は係数＝POB_COEF と試験の上書きが効く）。成長 0 なら行はあるが機構は回らない。
        c.OpiumAddictionGrowth = 0;
        var z = Habit.Of(c, Catalog.OpiumId);
        Assert.True(z.Forms); Assert.False(z.Active);
    }

    [Fact]
    public void 中毒度の更新は成長_減衰_0から上限の手の算術()
    {
        var h = Habit.Of(new Coefficients(), Catalog.OpiumId);
        Assert.Equal(0.079, h.Forms ? Habit.NextStock(h, 0.05, 0.3) : double.NaN, 12);   // 0.05＋0.1×0.3−0.02×0.05
        Assert.Equal(0.2, Habit.NextStock(h, 0.19, 1.0), 12);                           // 0.19＋0.1−0.0038＝0.2862 → 上限 0.2
        Assert.Equal(0.0098, Habit.NextStock(h, 0.01, 0.0), 12);                         // 消費が無ければ 2% ずつ減る
        Assert.Equal(0.0, Habit.NextStock(h, 0.0, 0.0), 12);
        // 習慣性の無い財の行（全て 0）：在庫は 0 に潰れる（行を持たない財では呼ばれない＝呼び手の Forms で守る）
        Assert.Equal(0.0, Habit.NextStock(default, 0.05, 0.3), 12);
    }

    [Fact]
    public void 労働の倍率は害_床0_5_成長0と在庫0では蝕まない()
    {
        var c = new Coefficients();
        var r = new Region { Name = "t" };
        r.OpiumAddiction = 0.05;
        Assert.Equal(0.95, Habit.LaborFactor(c, r), 12);   // 1−1.0×0.05
        r.OpiumAddiction = 0.2;
        Assert.Equal(0.8, Habit.LaborFactor(c, r), 12);
        r.OpiumAddiction = 0.9;
        Assert.Equal(0.5, Habit.LaborFactor(c, r), 12);    // 床
        r.OpiumAddiction = 0.0;
        Assert.Equal(1.0, Habit.LaborFactor(c, r));
        r.OpiumAddiction = 0.05; c.OpiumAddictionGrowth = 0;
        Assert.Equal(1.0, Habit.LaborFactor(c, r));        // 主フラグ（成長）が 0 なら害は出ない
        c.OpiumAddictionGrowth = 0.1; c.OpiumLaborPenalty = 2.0;
        Assert.Equal(0.9, Habit.LaborFactor(c, r), 12);    // 害の係数が追う
    }

    /// <summary>1か国の盤面で EconomyStep.Run を1回。全ての POP の貯蓄を 0 にして、州の中毒度 <paramref name="stock0"/> を置く。</summary>
    private static (Region Region, Coefficients Coef) OneMonth(double stock0, Action<Coefficients> tweak)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        tweak(w.Coef);
        w.Region.OpiumAddiction = stock0;
        EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
        return (w.Region, w.Coef);
    }

    [Fact]
    public void 成長0でも減衰は走る_行を持たない世界では在庫に触らない()
    {
        // 成長 0・減衰 0.02：1か月で 0.05 → 0.05−0.02×0.05＝0.049（消費の量に依らない＝成長の項は 0×消費）。旧の更新は成長>0 で守っていない（財の番号だけ）。
        var (r0, _) = OneMonth(0.05, c => c.OpiumAddictionGrowth = 0);
        Assert.Equal(0.049, r0.OpiumAddiction, 12);
        // 既定の成長 0.1：消費が正なら減衰の後より大きい（0.049 以上・上限 0.2 以下）
        var (r1, _) = OneMonth(0.05, c => { });
        Assert.InRange(r1.OpiumAddiction, 0.049 - 1e-12, 0.2);
        // 習慣性の行を持つ財が無い世界（アヘンの番号を存在しない財へ）：在庫は 1 か月後もそのまま＝他の財の経路が在庫に触れない
        var (r2, _) = OneMonth(0.05, c => c.OpiumGoodId = -1);
        Assert.Equal(0.05, r2.OpiumAddiction);
    }

    /// <summary>1か国の盤面で EconomyStep.Run を1回して、アヘンの州の市場の需要（LastDemand）を返す。奢侈品の所得弾力 ε と成長を変えて比べる（POP の所得は階級ごとに r が違う）。</summary>
    private static double OpiumDemand(double epsilon, double growth)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.LuxuryIncomeElasticity = epsilon;
        w.Coef.OpiumAddictionGrowth = growth;
        w.WorldRealIncomePerCapitaPerMonth = 10.0;
        w.PriceLevel = 2.0;
        foreach (var p in w.AllPops())
        {
            p.SetSavings(1e12);
            double r = p.ClassId == PopClass.Noble ? 0.25 : p.ClassId == PopClass.Peasant ? 4.0 : 1.0;
            p.LastEarnedPerMonth = new MoneyPerMonth(r * 10.0 * p.Size * 2.0);
        }
        w.Region.OpiumAddiction = 0.05;
        EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
        return w.Region.Markets[Catalog.OpiumId].LastDemand;
    }

    [Fact]
    public void アヘンは奢侈品の所得弾力の対象外_成長が0でも_習慣性の行で除く()
    {
        // 所得弾力 ε を 0 → 0.5 にしても、アヘンの需要は 1 ビットも変わらない（階級ごとの r が違うので、掛かれば必ず変わる）。
        // ⚠成長 0 でも除外は外れない（除外は財の行の有無＝番号で決まり、機構のフラグ〔成長>0〕ではない）。
        foreach (double growth in new[] { 0.1, 0.0 })
        {
            double a = OpiumDemand(0.0, growth), b = OpiumDemand(0.5, growth);
            Assert.True(a > 0, $"前提が崩れた：成長 {growth} でアヘンの需要が立っていない（主張が空洞）");
            Assert.Equal(a, b);
        }
    }

    /// <summary>1か国の盤面で EconomyStep.Run を1回して、労働力の州の市場の産出（LastProduced）を返す。州の中毒度 <paramref name="stock"/>・成長 <paramref name="growth"/>。</summary>
    private static double LaborProduced(double stock, double growth)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.OpiumAddictionGrowth = growth;
        // 労働供給は各 POP の初回の値（work0）で正規化される＝1回目は中毒度 0 で基準を捕まえ、2回目に中毒度を置いて比べる
        w.Region.OpiumAddiction = 0.0;
        EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
        w.Region.OpiumAddiction = stock;
        EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
        return w.Region.Markets[Catalog.LaborId].LastProduced;
    }

    [Fact]
    public void 依存は労働力の産出を蝕む_成長が0なら蝕まない_手の算術の上限と下限()
    {
        double clean = LaborProduced(0.0, 0.1), sick = LaborProduced(0.2, 0.1), noFlag = LaborProduced(0.2, 0.0);
        Assert.True(clean > 0, "前提が崩れた：労働力の産出が 0（主張が空洞）");
        // 成長 0 なら労働は蝕まれない（中毒度はアヘンの需要の上乗せとして州の家計を少し動かすので、完全なビット一致ではなく 1e-4 以内）
        Assert.InRange(noFlag / clean, 1.0 - 1e-4, 1.0 + 1e-4);
        // 成人の労働供給だけに ×(1−1.0×0.2)＝0.8 が掛かる（児童労働は蝕まれない）＝産出の比は 0.8 より大きく、蝕まれない場合より 5% 以上小さい
        double ratio = sick / clean;
        Assert.InRange(ratio, 0.8, 0.95);
    }

    /// <summary>★(344) 反証役 op〔v〕：依存の在庫は州の 1 本（<see cref="Region.OpiumAddiction"/>）だけ＝<see cref="Habit.StockOf"/> と更新は、行を持つ財ならどれもこの 1 本を読み書きする。
    /// 2 財目の行を <see cref="Habit.Of"/> に足すと、アヘンの在庫を黙って共有し上書きする＝在庫を州×財へ分けるまで、行を持つ財は 1 つまで（分けた回にこの番人を外す）。</summary>
    [Fact]
    public void 習慣性の行を持つ財は在庫を州と財へ分けるまで1つだけ()
    {
        var c = new Coefficients();
        int n = Catalog.Goods.Count(g => Habit.Of(c, g.Id).Forms);
        Assert.True(n <= 1, $"習慣性の行を持つ財が {n} 個＝在庫が州の 1 本のまま共有される（先に在庫を州×財へ分ける）");
    }

    // ---------------------------------------------------------------- (342) 段1：アヘンの表 opium_s ----------------------------------------------------------------

    private static MultiNationSim MakeMatch() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    /// <summary>英の最初の活きた州に中毒度 0.05 を置いて <paramref name="quarters"/> 四半期。計器を出すなら opium_s の読み先を返す。最後の英の州の中毒度と国庫のビット列も返す。</summary>
    private static (long Digest, string? Dir) RunMatch(int quarters, bool instr)
    {
        string? dir = instr ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_opium_" + Guid.NewGuid().ToString("N")) : null;
        if (instr) Instruments.Configure(dir, null);
        var sim = MakeMatch();
        var uk = sim.Nations[2].World;
        var seeded = uk.Regions.First();
        seeded.OpiumAddiction = 0.05;
        // ★(436) 合流：合わせた木（cl＋sb＋dq の点火）では、この 4 国の試合でロンドンにアヘンが 30 四半期以上届かなくなった（産地の無い盤面で輸入の軌道が動いた・需要は立っている）＝
        //   販売量の計器の口の番人が軌道に依らないように、種を置いた州の市場にアヘンの在庫を置く（計器なしと計器つきの両方に同じ盤面＝軌道の一致の主張は変わらない）。
        foreach (var rgn in uk.Regions) rgn.Markets[Catalog.OpiumId].Stock = 5000;   // ★(687) 種を置いた州だけ→英の全ての州：UrbanCapOff の点火で最初の 12 四半期の軌道が動き、種の州だけでは販売量が 0 になった（販売量の計器の口の番人は国の行のどれかが正であれば足りる）
        seeded.Markets[Catalog.OpiumId].Stock = 5000;   // ★(504) 50→5000：①③の点火（主食の買い方が変わり最初の 12 四半期の軌道が動いた）で、四半期の末の月の販売量が 12 四半期とも 0 になった（原因は未特定＝推測は在庫 50 の売り切れ）＝計器の口の番人は在庫の多さに依らないようにした
        try
        {
            if (instr) Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            if (instr) Instruments.Phase(sim, 1789UL, "warm");
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (instr) Instruments.Quarter(sim, 1789UL, q); }
        }
        finally { if (instr) { Instruments.Close(sim); Instruments.Configure(null, null); } }
        long d = BitConverter.DoubleToInt64Bits(seeded.OpiumAddiction) * 31 + BitConverter.DoubleToInt64Bits(uk.Treasury.Value);
        return (d, dir);
    }

    [Fact]
    public void アヘンの表は州と国の行が揃い_国の中毒度は州の和に合い_計器を出しても軌道は変わらない()
    {
        const int quarters = 12;
        var (plain, _) = RunMatch(quarters, instr: false);
        var (withIns, dir) = RunMatch(quarters, instr: true);
        try
        {
            Assert.Equal(plain, withIns);   // 計器は読むだけ＝種を置いた州の中毒度と英の国庫の 2 値がビットで一致（★(344) 言い方を直した：全軌道の一致の根拠は計器つきの帯〔_ins〕が計器なしの帯と全行一致すること）
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir!, "opium_s1789.tsv"));
            var h = lines[0].Split('	'); var rows = lines.Skip(1).Select(l => l.Split('	')).ToList();
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            int I(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
            double D(string[] r, string c) { var v = r[I(c)]; return v switch { "nan" => double.NaN, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) }; }
            var region = rows.Where(r => r[I("scope")] == "R").ToList(); var nation = rows.Where(r => r[I("scope")] == "N").ToList();
            Assert.True(region.Count >= quarters, "種を置いた州が毎四半期の行に出ていない（中毒度は 0 にならない）");
            Assert.Equal(region.Count, region.Select(r => (r[I("quarter")], r[I("nationIndex")], r[I("region")])).Distinct().Count());
            var seededRows = region.Where(r => r[I("nationIndex")] == "2").ToList();
            Assert.True(seededRows.Count >= quarters);
            // ★(344) 反証役 op〔v〕：販売量を積む計器の口（Instruments.HabitSold）が消えても緑のままだった＝種を置いた国の州の行で販売量が正の行があること
            Assert.True(seededRows.Any(r => D(r, "soldPerMonth") > 0), "英の州の販売量が全て 0（置いたアヘンの在庫は助走の間に売り切れ・ロンドンに産地は無い＝輸入が数える 12 四半期に届かないとこうなる〔(697) の診断：在庫をロンドンだけに置くと旗 1 では 12 四半期とも届かず〔旗 0 は 8 四半期目〕・英の全ての州に置くと 12 四半期目に届く〕）：" + string.Join(" ", seededRows.Take(12).Select(r => $"[q{r[I("quarter")]} 中毒{D(r, "addictionIndex"):F3} 販売{D(r, "soldPerMonth"):F3} 価格{D(r, "unitPrice"):F3}]")));
            foreach (var r in region)
            {
                double a = D(r, "addictionIndex");
                Assert.InRange(a, 0, 0.2 + 1e-12);                                                  // 上限（係数の既定 0.2）
                Assert.InRange(D(r, "laborFactorIndex"), Math.Max(0.5, 1.0 - a) - 1e-9, Math.Max(0.5, 1.0 - a) + 1e-9);   // 労働の倍率は手の算術（害 1.0・床 0.5）。★(383) 桁の丸め（8 桁）で境界の値が 1e-8 ずれた→★(387) 絶対の許容差 1e-9（表は有効数字 10 桁＝丸めの境界に依らない・反証役 ae〔bv〕低）
                Assert.True(D(r, "soldPerMonth") >= 0);
                Assert.InRange(D(r, "productionFactorIndex"), 0, 1);
            }
            // 国の中毒度（人口加重）は、州の行の中毒度×人口の和÷国の活きた州の人口（nation_s の人口＝別の走査）に合う
            var nl = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir!, "nation_s1789.tsv"));
            var nh = nl[0].Split('	'); var nrows = nl.Skip(1).Select(l => l.Split('	')).ToList();
            int nq = Array.IndexOf(nh, "quarter"), nx = Array.IndexOf(nh, "nationIndex"), np = Array.IndexOf(nh, "populationK");
            var natPop = nrows.ToDictionary(r => (r[nq], r[nx]), r => double.Parse(r[np], System.Globalization.CultureInfo.InvariantCulture));
            foreach (var n in nation)
            {
                var key = (n[I("quarter")], n[I("nationIndex")]);
                double num = region.Where(r => (r[I("quarter")], r[I("nationIndex")]) == key).Sum(r => D(r, "addictionIndex") * D(r, "populationK"));
                double expect = natPop[key] > 0 ? num / natPop[key] : 0;
                Assert.InRange(D(n, "nationalAddictionIndex") - expect, -1e-7 * Math.Max(1, expect), 1e-7 * Math.Max(1, expect));
                double soldSum = region.Where(r => (r[I("quarter")], r[I("nationIndex")]) == key).Sum(r => D(r, "soldPerMonth"));
                Assert.InRange(D(n, "soldTotalPerMonth") - soldSum, -1e-7 * Math.Max(1, soldSum), 1e-7 * Math.Max(1, soldSum));
                Assert.InRange(D(n, "possessionLawCode"), 0, 2);
                Assert.Contains(n[I("exportBanFlag")], new[] { "0", "1" });
                Assert.True(D(n, "tensionAddedPerQuarter") >= 0 && D(n, "tensionPairMonthsCount") >= 0);
            }
            Assert.True(nation.Count >= quarters, "種を置いた国の国×四半期の行が出ていない");
        }
        finally { try { System.IO.Directory.Delete(dir!, true); } catch { } }
    }
}
