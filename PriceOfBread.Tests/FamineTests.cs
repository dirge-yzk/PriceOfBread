using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class FamineTests
{
    // ★2026-09-30 (131)：飢えの死は POP の飢えの上乗せの1本＝OFF の腕はその強さを0に（旧＝局地飢饉 RegionalFamineMortality=0）。
    // ★2026-10-01 (143)：上乗せは飢えの深さの絶対の率（FamineExcessMortality×深さ^FamineExcessMortalityExponent・旧＝平常の死亡率×FamineMortalityConcentration×深さ）。
    private static Scenario Famine(ulong s, double mortality = -1, double birthRate = -1)
    {
        var c = new Coefficients();
        if (mortality >= 0) c.FamineExcessMortality = mortality;
        if (birthRate >= 0) c.CrudeBirthRate = birthRate;
        return new Scenario { Seed = s, Kind = ScenarioKind.RegionalFamine, Coef = c };
    }

    [Fact]
    public void 局地飢饉で食料不足の州だけが減り豊富な州は減らない()   // §A-3拡張：局地飢饉（★2026-09-30 (131) 相対的剥奪の死は削除＝POP の飢えの上乗せが運ぶ）
    {
        var on = new Simulation(Famine(1), new NullAgent());
        double rich0 = on.World.Regions[0].SumPopulation();
        double poor0 = on.World.Regions[1].SumPopulation();
        on.Run(120);   // 30年
        double richOn = on.World.Regions[0].SumPopulation();
        double poorOn = on.World.Regions[1].SumPopulation();

        // 局地化：不足州（州1）は飢饉で明確に減り、豊作州（州0）はほぼ減らない（道連れにならない）
        Assert.True(poorOn < poor0 * 0.95, $"食料不足の州が飢饉で減らない（{poor0:F0}→{poorOn:F0}）");
        Assert.True(richOn > rich0 * 0.95, $"食料豊富な州まで道連れに減った（{rich0:F0}→{richOn:F0}）＝局地化していない");

        // 飢えの上乗せの寄与を分離：上乗せ OFF（FamineExcessMortality=0）より ON の方が不足州がさらに減る
        var off = new Simulation(Famine(1, mortality: 0), new NullAgent());
        off.Run(120);
        Assert.True(poorOn < off.World.Regions[1].SumPopulation(),
            $"局地飢饉ショックが不足州を追加で減らしていない（ON {poorOn:F0} ≥ OFF {off.World.Regions[1].SumPopulation():F0}）");
    }

    [Fact]
    public void 局地飢饉でも人口保存は保たれる()
    {
        var sim = new Simulation(Famine(2), new NullAgent());
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(120));
        Assert.Null(ex);   // §11.6（保存・境界）が全ティック保持＝飢饉死も PopulationDelta 台帳で追跡
        Assert.True(System.Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "局地飢饉込みの人口保存の破れ");
    }

    [Fact]
    public void 回B_c_物理の経路は慢性の不足にも効き続ける()
    {
        // ★回B（2026-09-22）：飢餓の駆動（政治）は平常 Pop.FoodRef からの落ち込みを測るようになったが、飢饉死・疫病の脆弱層集中
        //   （と国家間移民の押し出し＝MultiNationSim が州の FoodFill を読む）は**絶対水準**を読む＝慢性の不足はこれまでどおり人を殺す。
        //   盤面＝不足州（充足は初期0.15→3年で0.58＝ずっと飢饉死の門 FamineDeathFillFloor の下）。その3年のあいだ毎四半期：
        //   ①不足州の pop は飢饉死の門の下に居る（慢性）②政治の駆動は0（平常＝初回の低さを捕まえた後、充足はそこから落ちていない）
        //   ③物理の経路は平常を1ビットも読まない＝平常を「豊かさに慣れた 1.0」に置いた腕と人口の軌道がビット一致。
        //   そして④不足州は3年で2割以上減る（物理の経路が慢性の不足に効き続ける）。
        var a = new Simulation(Famine(1), new NullAgent());
        var b = new Simulation(Famine(1), new NullAgent());
        foreach (var p in b.World.AllPops()) p.FoodRef = 1.0;
        var c = a.World.Coef;
        var poor = a.World.Regions[1];
        double poor0 = poor.SumPopulation();
        for (int q = 0; q < 12; q++)
        {
            a.Run(1); b.Run(1);
            // 平常の捕捉＝Politics.Step の初回と同じ作法（今の充足）。この盤面は政治なし＝力学は平常を書かないので、ここで置く。
            if (q == 0) foreach (var p in poor.Pops) if (p.FoodNeed > 1e-9) p.FoodRef = System.Math.Min(1.0, p.FoodMet / p.FoodNeed);
            foreach (var p in poor.Pops)
            {
                if (!(p.FoodNeed > 1e-9)) continue;
                double fill = p.FoodMet / p.FoodNeed;
                Assert.True(fill < c.FamineDeathFillFloor, $"前提：不足州が慢性的に食えていない（{q}四半期目 {p.ClassName} 充足 {fill:F3}）");
                Assert.Equal(0.0, Politics.FamineDrive(p, c));   // 慢性の低さは政治の駆動にならない
            }
            for (int i = 0; i < a.World.Regions.Count; i++)
                Assert.Equal(a.World.Regions[i].SumPopulation(), b.World.Regions[i].SumPopulation());   // 平常を読む物理の経路は無い（厳密一致）
        }
        Assert.True(poor.SumPopulation() < poor0 * 0.8, $"慢性の不足で不足州が減らない（{poor0:F0}→{poor.SumPopulation():F0}）＝物理の経路が止まった");
    }

    [Fact]
    public void 飢えた植民地の州は飢えの死だけでピークの10分の1未満へ落ちない_飢えの深さの絶対の率()
    {
        // ★2026-09-30 (131)→2026-10-01 (143)（作者 2026-09-30「州から人が抜けきるというのは現実的ではないです。これは不具合とみなしてください。」）。
        //   盤面＝豊かな本国の州（全 POP の充足1）と飢えた植民地の州（全 POP の充足0.58＝飢えの深さ 0.275）の1国。充足0.58 は (140) の木の計器で
        //   空洞化した州の飢えた POP の平均（州の7割が飢え・百四十年続いた・`measurements/fam_20261001`）。
        //   毎月、充足を置いて人口動態（DemographicStep）だけを167年（2004か月）回す＝飢えは人が減っても和らがない（(131) の計器＝植民地の州の飢えは
        //   値段と購買力で決まり、人口が1/10 へ落ちても充足は動かない）。扶養力は動かない盤面＝国が伸びきった後の厳しい形。
        //   ①植民地の州は自分のピークの1/10 未満へ落ちない ②前提＝飢えは効いている（植民地の州は本国の州より伸びが低い）③人口保存。
        //   旧（(131)＝平常の死亡率×3×深さ）では慢性の浅い飢え（深さ0.275）が年 約25‰ で州を削った・深さに比例（指数1）でも同じ桁。
        var sim = new Simulation(Famine(1), new NullAgent());
        var w = sim.World;
        var home = w.Regions[0]; var col = w.Regions[1];
        void SetFill()
        {
            foreach (var r in w.Regions)
                foreach (var p in r.Pops)
                {
                    double fill = ReferenceEquals(r, home) ? 1.0 : 0.58;
                    p.FoodNeed = 0.8 * p.Size; p.FoodMet = fill * p.FoodNeed;
                }
        }
        double home0 = home.SumPopulation(), col0 = col.SumPopulation(), colPeak = col0, colMinShare = 1.0;
        double base0 = w.TotalPopulation - w.PopulationDelta;
        for (int m = 0; m < 2004; m++)
        {
            SetFill();
            sim.DemographicStep();
            double cp = col.SumPopulation();
            if (cp > colPeak) colPeak = cp;
            colMinShare = System.Math.Min(colMinShare, cp / colPeak);
        }
        double homeRatio = home.SumPopulation() / home0, colRatio = col.SumPopulation() / col0;
        Assert.True(colRatio < homeRatio * 0.9, $"前提：飢えが効いていない（植民地の州 ×{colRatio:F3}・本国の州 ×{homeRatio:F3}）");
        Assert.True(colMinShare >= 0.1, $"飢えた植民地の州がピークの1/10 未満へ落ちた（最小 {colMinShare:F3}・終わり ×{colRatio:F3}・本国 ×{homeRatio:F3}）");
        Assert.True(System.Math.Abs(w.TotalPopulation - (base0 + w.PopulationDelta)) < 1e-6 * base0, "人口保存の破れ");
    }

    [Fact]
    public void 飢えの超過死亡は平常の死亡率によらない絶対の率_出生率の違う国でも同じ飢えで同じだけ死ぬ()
    {
        // ★2026-10-01 (143)：飢えの超過死亡＝FamineExcessMortality÷12×深さ^指数（深さ＝(門−充足)÷門）＝平常の死亡率を読まない。
        //   旧（(131)）＝平常の死亡率×濃さ×深さ＝出生と死の多い国ほど同じ飢えで多く死んだ（年齢の層で出生と死が史実へ近づいた分だけ飢えの死が増え、
        //   r138 で飢えによる州の空洞化が2→16件）。盤面＝粗出生率 0.035 と 0.050 の2つの国（平常の死亡率が違う）。各国で同じ構成の農民の対
        //   （本国の州＝充足1・不足州＝充足0.4）を1か月＝1人あたりの増減の差がその国の飢えの上乗せ（1人あたり）＝両国で同じ・式の値と一致。
        double Excess(double birthRate, out double deathsPerHead)
        {
            var sim = new Simulation(Famine(1, birthRate: birthRate), new NullAgent());
            var w = sim.World;
            Pop Of(int region) { foreach (var p in w.Regions[region].Pops) if (p.ClassId == 0) return p; throw new System.InvalidOperationException(); }
            var fed = Of(0); var hungry = Of(1);
            foreach (var r in w.Regions) foreach (var p in r.Pops) { p.FoodNeed = 0.8 * p.Size; p.FoodMet = p.FoodNeed; }
            hungry.FoodMet = 0.4 * hungry.FoodNeed;
            double f0 = fed.Size, h0 = hungry.Size, d0 = w.DiagDeaths;
            sim.DemographicStep();
            deathsPerHead = (w.DiagDeaths - d0) / w.TotalPopulation;
            return (fed.Size - f0) / f0 - (hungry.Size - h0) / h0;
        }
        var c = new Coefficients();
        double expect = c.FamineExcessMortality / 12.0 * System.Math.Pow((c.FamineDeathFillFloor - 0.4) / c.FamineDeathFillFloor, c.FamineExcessMortalityExponent);
        double lo = Excess(0.035, out double dLo), hi = Excess(0.050, out double dHi);
        Assert.True(dHi > dLo * 1.2, $"前提：平常の死亡率が違う国になっていない（1人あたりの死 {dLo:E3}・{dHi:E3}）");
        Assert.True(System.Math.Abs(lo - expect) < 1e-9 && System.Math.Abs(hi - expect) < 1e-9,
            $"飢えの上乗せが飢えの深さの絶対の率でない（出生 0.035 の国 {lo:E6}・0.050 の国 {hi:E6}・式 {expect:E6}）");
    }

    [Fact]
    public void 飢饉の線の下で農村より食べられない都市のPOPへは農村から移らない_農村がもっと飢えていれば移る()
    {
        // ★2026-10-01 (143)（線2 後半・飢えで州が痩せる型の残り）：農村→都市の移動（PopDecision）は貨幣の所得の差で目標の都市の割合を決める。
        //   旧＝都市の POP が飢えていても（植民地の工場労働者＝主食の充足 0.22・失業 60%）目標へ引かれ続け、農民が飢えた都市へ移って飢えの死で消えた（流しの管）。
        //   盤面＝同じ州の農民（農村）と工場労働者（都市）。都市の所得を農村の2倍に置き（旧なら必ず都市へ流れる）、充足だけを振って1回：
        //   ①都市 0.2・農村 0.9（都市が飢饉の線の下で農村より飢える）＝移らない ②都市 0.9・農村 0.9（都市が線の上）＝移る
        //   ③都市 0.6・農村 0.4（農村がもっと飢える＝町へ逃げる）＝移る。
        double Moved(double fillRural, double fillUrban)
        {
            var sim = new Simulation(Famine(1), new NullAgent());
            var w = sim.World; var r = w.Regions[0];
            Pop Of(int cls) { foreach (var p in r.Pops) if (p.ClassId == cls) return p; throw new System.InvalidOperationException(); }
            var rural = Of(0); var urban = Of(1);
            rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = fillRural * rural.FoodNeed; rural.EarnedPerMonth = 0.1 * rural.Size;
            urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = fillUrban * urban.FoodNeed; urban.EarnedPerMonth = 0.2 * urban.Size;
            double u0 = urban.Size;
            PopDecision.Run(r, w.Coef);
            return urban.Size - u0;
        }
        double starving = Moved(0.9, 0.2), fed = Moved(0.9, 0.9), refuge = Moved(0.4, 0.6);
        Assert.True(fed > 0, $"前提：都市の所得が高く食べられる時に農村から移らない（{fed:E3}）");
        Assert.True(refuge > 0, $"農村がもっと飢えている時に町へ逃げない（{refuge:E3}）");
        Assert.True(starving <= 0, $"飢饉の線の下で農村より食べられない都市の POP へ農村から移った（{starving:E3}）");
    }

    [Fact]
    public void 飢えの上乗せは飢饉死の門で連続_門のすぐ下は平常とほぼ同じ()
    {
        // ★2026-09-30 (131)：上乗せは飢えの深さ（(門−充足)÷門）の関数（(143) から絶対の率＝FamineExcessMortality×深さ^指数）。旧の重みの不足（1−充足）のままでは
        //   門（0.8）で 0→0.2 へ跳ぶ＝上乗せの率にすると門のすぐ下の POP に平常の6割の超過死亡が段で立つ。
        //   盤面＝同じ階級（同じ構成＝1人あたりの出生と平常の死が同じ）の POP の対を2組：①充足0.81（門の上）と0.79（門のすぐ下）②充足1.0 と0.4（深い飢え）。
        //   1か月の人口動態の1人あたりの増減の差＝その POP の上乗せ（1人あたり）。①の差は②の差の1割未満（連続なら 0.0125÷0.5＝2.5%・跳ぶと約35%）。
        var sim = new Simulation(Famine(1), new NullAgent());
        var w = sim.World;
        Pop Of(int region, int cls) { foreach (var p in w.Regions[region].Pops) if (p.ClassId == cls) return p; throw new System.InvalidOperationException(); }
        var above = Of(0, 0); var below = Of(1, 0);      // 農民の対（門の上と門のすぐ下）
        var deep = Of(0, 1); var fed = Of(1, 1);         // 工場労働者の対（深い飢えと満腹）
        foreach (var r in w.Regions) foreach (var p in r.Pops) { p.FoodNeed = 0.8 * p.Size; p.FoodMet = p.FoodNeed; }
        above.FoodMet = 0.81 * above.FoodNeed; below.FoodMet = 0.79 * below.FoodNeed; deep.FoodMet = 0.4 * deep.FoodNeed;
        double a0 = above.Size, b0 = below.Size, d0 = deep.Size, f0 = fed.Size;
        sim.DemographicStep();
        double gapGate = (above.Size - a0) / a0 - (below.Size - b0) / b0;
        double gapDeep = (fed.Size - f0) / f0 - (deep.Size - d0) / d0;
        Assert.True(gapDeep > 0, $"前提：深い飢えの上乗せが立っていない（差 {gapDeep:E3}）");
        Assert.True(gapGate < 0.1 * gapDeep, $"飢えの上乗せが門で跳ぶ（門の上下の差 {gapGate:E3}・深い飢えの差 {gapDeep:E3}＝比 {gapGate / gapDeep:F3}）");
    }

    [Fact]
    public void 年齢コホート_実国で老年人口が形成される()   // ①年齢コホート（★2026-09-30 段4e 後半＝年齢の層・老年比の上限0.15〔天井〕を撤去）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        sim.Run(200);   // 50年：加齢と年齢の重みの死で老年の層が保たれる（1783年は標準のピラミッドの 7.5%）
        double oldSize = 0, totalSize = 0;
        foreach (var p in sim.World.AllPops()) if (!p.IsSoldier) { oldSize += p.Size * p.AgeShare(60, double.PositiveInfinity); totalSize += p.Size; }
        Assert.True(totalSize > 0, "人口が消えた");
        Assert.InRange(oldSize / totalSize, 0.03, 0.2);   // 天井なしで溜まりすぎず・加齢で消えもしない（前近代〜転換期の 60歳以上＝5〜15%）
    }

    [Fact]
    public void 地域物価_複数州の実国では州ごとに物価水準が乖離する()   // ②集計粒度：地域別の貨幣的物価水準
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        sim.Run(40);   // 10年
        var mults = new System.Collections.Generic.List<double>();
        foreach (var r in sim.World.Regions) if (r.State == RegionState.Active) mults.Add(r.LocalPriceMultiplier);
        Assert.True(mults.Count >= 2, "複数州の前提が崩れた（実国が単一州）");
        double max = mults[0], min = mults[0];
        foreach (var m in mults) { if (m > max) max = m; if (m < min) min = m; }
        // 現金の厚みの差で州ごとの物価乗数が一律1.0でなく乖離する（金融中心は高・辺境は低）。
        // 閾値0.02→0.01（B6・2026-07-19）：新財（馬等）の生産者が全州に立ち地方州へ薄く所得を配る＝乖離幅が0.021→0.015へ縮んだ
        // （機構は生存・地方産業が現金格差を狭める実在の経済効果）。ゼロ乖離（機構死）は引き続き検出する。
        // 閾値0.01→0.005（2026-09-26 ㊽ 国内価格の旬次化）：乖離幅そのものは1本の軌道の水準＝刻み h（PriceFlowStepDays）を
        //   1/2/3/5/6/10/15 と振ると 0.0103/0.0100/0.0105/0.0099/0.0121/0.0104/0.0109 と非単調に散る（旧来の日次 h=1 でも縁から2.5%）。
        //   番人が見るべきは機構死＝全州が一律1.0（RegionalPriceLevelWeight=0 で乖離0＝この閾値でも赤）で、水準は帯が見る。
        Assert.True(max - min > 0.005, $"州ごとの物価水準が乖離していない（{min:F3}〜{max:F3}）＝地域別物価が効いていない");
    }
}
