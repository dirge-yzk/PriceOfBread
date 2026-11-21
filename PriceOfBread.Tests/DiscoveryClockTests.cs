using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(461) 発見の時計の細い傾斜の番人。係数 <see cref="Coefficients.DiscoveryClockWorldPerMonth"/>（W0）・<see cref="Coefficients.DiscoveryClockRampWidth"/>（w）・<see cref="Coefficients.DiscoveryOwnHalfPerMonth"/>（Am）
/// （事前登録 measurements/dk_20261020_prereg.txt）。W0＝0（既定）＝(415) の式。W0 が正（技師の形の上）＝d＞0 の行の技術力＝70×S×C・S＝clamp((W÷(W0×d)−1)÷w＋1/2, 0, 1)
/// （W＝世界の高度労働力の雇用の合計〔全ての財の和〕・全ての行と国で共通）・C＝A÷(A＋Am)（A＝自国の親の産業の技師）・押しは掛けない。d≦0 の行は今の式。
/// 機構だけを検査する（W0・Am の値・解禁の年の並びは創発＝成績表が見る）。期待値は番人の側で手計算の数字か、生産者を直接たどって組む（本体の関数を使わない）。</summary>
[Collection("Instruments")]
public class DiscoveryClockTests
{
    private static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == name);

    private const double TestOwn = 6.0;   // 検査用の Am（較正値ではない）
    private const double TestWidth = 0.1;

    private static MultiNationSim Board(double clock, double own)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        sim.Coef.DiscoveryClockWorldPerMonth = clock; sim.Coef.DiscoveryOwnHalfPerMonth = own;   // 多国の判定が読む共有の係数
        foreach (var n in sim.Nations) { n.World.Coef.DiscoveryClockWorldPerMonth = clock; n.World.Coef.DiscoveryOwnHalfPerMonth = own; }
        sim.StepQuarter();
        return sim;
    }

    private static readonly Lazy<MultiNationSim> BoardOff = new(() => Board(0, TestOwn));   // W0＝0 なら Am があっても既定（(415) の式）
    /// <summary>この盤面の 1 四半期目の世界の合計 W に対し、d＝7 の行が傾斜の真ん中（S＝1/2）に来る W0（＝W÷7）。検査用で較正値ではない。</summary>
    private static readonly Lazy<double> TestClock = new(() => HandWorld(BoardOff.Value) / 7.0);
    private static readonly Lazy<MultiNationSim> BoardOn = new(() => Board(TestClock.Value, TestOwn));
    /// <summary>電動機（d＝10）が傾斜の真ん中（S＝1/2）に来る W0（＝W÷10）の盤面を、電動機の親の旗が 0 と正の 2 通り作る（d＝7 用の TestClock では電動機の S が 0 で検査が空回りする）。</summary>
    private static readonly Lazy<double> TestClockMotor = new(() => HandWorld(BoardOff.Value) / 10.0);
    private static readonly Lazy<MultiNationSim> BoardMotorOff = new(() => Board(TestClockMotor.Value, TestOwn));

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    private static Producer? RepOf(World w, int row)
        => w.Regions.Where(rg => rg.State == RegionState.Active).SelectMany(rg => rg.Producers).FirstOrDefault(p => p.DiscoveryRow == row);

    private static double AdvQty(Producer p)
    {
        foreach (var inp in p.Inputs) if (inp.GoodId == Catalog.AdvLaborId) return inp.QtyPerOutput;
        return 0;
    }

    /// <summary>行の親の財の集合（行の財か置き換える財・<paramref name="extraParent"/> が 0 以上ならその財も足す）＋代表のレシピの労働でない投入。</summary>
    private static HashSet<int> ParentGoods(int row, Producer rp, int extraParent = -1)
    {
        var d = Catalog.Discoveries[row];
        int g0 = d.Replaces >= 0 ? d.Replaces : d.Good;
        var goods = new HashSet<int> { g0 };
        if (extraParent >= 0) goods.Add(extraParent);
        foreach (var inp in rp.Inputs)
            if (inp.GoodId != Catalog.LaborId && inp.GoodId != Catalog.AdvLaborId && inp.GoodId != Catalog.FarmLaborId) goods.Add(inp.GoodId);
        return goods;
    }

    /// <summary>番人の側で組んだ国の A＝親の財を作る生産者のうち門の開いたものの 実産出×高度労働力の投入量 の和（本体の <c>eng[]</c> を使わない）。</summary>
    private static double HandA(World w, HashSet<int> goods)
    {
        double a = 0;
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            foreach (var p in rg.Producers)
                if (goods.Contains(p.GoodId) && Discovery.Gate(rg, p) > 0 && p.Output > 0) a += p.Output * AdvQty(p);
        }
        return a;
    }

    /// <summary>番人の側で組んだ世界の高度労働力の雇用の合計 W＝全ての国の稼働中の州の、門の開いた全ての生産者の 実産出×高度労働力の投入量 の和（全ての財）。</summary>
    private static double HandWorld(MultiNationSim sim)
    {
        double t = 0;
        foreach (var n in sim.Nations)
            foreach (var rg in n.World.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                foreach (var p in rg.Producers)
                    if (Discovery.Gate(rg, p) > 0 && p.Output > 0) t += p.Output * AdvQty(p);
            }
        return t;
    }

    /// <summary>S の形（純粋な式・手計算の数字）：W0＝40・w＝0.1・転炉鋼（d＝7）で W＝280 が S＝1/2・266（＝280×0.95）が 0・294（＝280×1.05）が 1・287（＝280×1.025）が 0.75・273（＝280×0.975）が 0.25。
    /// 幅 w＝0.2 なら 0 と 1 の W は 252（＝280×0.9）と 308（＝280×1.1）。d≦0 の行は 1・W≦0 か NaN は 0・W に対して単調に増え 1 を超えない・同じ W で d が大きい行ほど小さい・W0 か w が正でなければ 0。
    /// C＝d≦0 で1・A≦0 か NaN で0・A に対して単調で1を超えない・A＝Am で半分・C(2A)/C(A)＝2(A＋Am)÷(2A＋Am)。
    /// ⚠壊し方＝区間の端を取り違える（0 と 1 の W が動く）・W＝0 で 1 を返す・d の向きを逆にする・分母から W0 を落とす・幅を無視する・C の分母から Am を落とす → 赤。</summary>
    [Fact]
    public void 時計Sは細ランプで_手計算の点で0_半分_1になり_Cは有界の飽和()
    {
        var c = new Coefficients { DiscoveryClockWorldPerMonth = 40.0, DiscoveryClockRampWidth = 0.1, DiscoveryOwnHalfPerMonth = TestOwn };
        int steam = Row("蒸気機関"), bess = Row("転炉鋼"), loco = Row("機関車");
        Assert.Equal(0, Catalog.Discoveries[steam].Decade - Catalog.DiscoveryBaseDecade);
        Assert.Equal(7, (Catalog.Discoveries[bess].Decade - Catalog.DiscoveryBaseDecade) / 10);
        Assert.Equal(1.0, Discovery.ClockStage(steam, 0, c));                    // d＝0 の行は W＝0 でも 1
        Assert.Equal(1.0, Discovery.OwnCapacity(steam, 0, c));
        Assert.Equal(0.0, Discovery.ClockStage(bess, 0, c));                     // W＝0 の世界は 0
        Assert.Equal(0.0, Discovery.ClockStage(bess, double.NaN, c));
        Assert.Equal(0.0, Discovery.ClockStage(bess, -3, c));
        Assert.Equal(0.0, Discovery.OwnCapacity(bess, 0, c));                   // A＝0 の国は 0
        Assert.Equal(0.0, Discovery.OwnCapacity(bess, double.NaN, c));
        Assert.Equal(0.0, Discovery.OwnCapacity(bess, -3, c));
        // 手計算の点（d＝7・W0＝40 → 中心 280）
        Assert.Equal(0.5, Discovery.ClockStage(bess, 280.0, c), 9);
        Assert.Equal(0.0, Discovery.ClockStage(bess, 266.0, c), 9);
        Assert.Equal(1.0, Discovery.ClockStage(bess, 294.0, c), 9);
        Assert.Equal(0.75, Discovery.ClockStage(bess, 287.0, c), 9);
        Assert.Equal(0.25, Discovery.ClockStage(bess, 273.0, c), 9);
        Assert.Equal(0.0, Discovery.ClockStage(bess, 100.0, c));                 // 端の外は 0 と 1 に張り付く（上限・下限は区間の端であって天井ではない）
        Assert.Equal(1.0, Discovery.ClockStage(bess, 1e9, c));
        // 幅を倍にすると 0 と 1 の W が 252 と 308 へ動く
        var c2 = new Coefficients { DiscoveryClockWorldPerMonth = 40.0, DiscoveryClockRampWidth = 0.2, DiscoveryOwnHalfPerMonth = TestOwn };
        Assert.Equal(0.0, Discovery.ClockStage(bess, 252.0, c2), 9);
        Assert.Equal(1.0, Discovery.ClockStage(bess, 308.0, c2), 9);
        Assert.Equal(0.5, Discovery.ClockStage(bess, 280.0, c2), 9);
        Assert.Equal(0.0, Discovery.ClockStage(bess, 280.0, new Coefficients { DiscoveryClockWorldPerMonth = 0, DiscoveryClockRampWidth = 0.1 }));   // W0 が正でなければ 0
        Assert.Equal(0.0, Discovery.ClockStage(bess, 280.0, new Coefficients { DiscoveryClockWorldPerMonth = 40, DiscoveryClockRampWidth = 0 }));    // 幅が正でなければ 0
        // 単調・同じ W で d が大きい行ほど小さい
        double prevS = -1, prevC = 0;
        foreach (double x in new[] { 100.0, 266.0, 272.0, 280.0, 288.0, 294.0, 1e4 })
        {
            double s = Discovery.ClockStage(bess, x, c);
            Assert.True(s >= prevS && s <= 1.0, $"W={x}：S が単調に増えず 1 を超えた（{s}）");
            prevS = s;
        }
        foreach (double x in new[] { 0.1, 1, 10, 100, 1e4, 1e8 })
        {
            double cc = Discovery.OwnCapacity(bess, x, c);
            Assert.True(cc > prevC && cc <= 1.0, $"A={x}：C が単調に増えず 1 を超えた（{cc}）");
            prevC = cc;
        }
        Assert.True(Catalog.Discoveries[bess].Decade > Catalog.Discoveries[loco].Decade);
        Assert.True(Discovery.ClockStage(bess, 200, c) < Discovery.ClockStage(loco, 200, c));   // 同じ W で d が大きい行ほど小さい（機関車 d＝4・中心 160）
        Assert.Equal(0.5, Discovery.OwnCapacity(bess, TestOwn, c), 12);         // A＝Am で半分
        // 国の大きさに対して有界：A を 2 倍にしたときの C の増分は A が大きいほど小さい（同じなら +33%）
        double Gain(double a) => Discovery.OwnCapacity(bess, 2 * a, c) / Discovery.OwnCapacity(bess, a, c) - 1;
        Assert.Equal(1.0 / 3, Gain(TestOwn), 12);
        Assert.Equal(2.0 * (8 + 1) / (2 * 8 + 1) - 1, Gain(8 * TestOwn), 12);
        Assert.True(Gain(30 * TestOwn) < Gain(8 * TestOwn) && Gain(8 * TestOwn) < Gain(TestOwn));
    }

    /// <summary>世界の合計 W は、番人の側で全ての国の全ての生産者を直接たどった 高度労働力の雇用の和（全ての財）と一致する（実盤面の今の状態）。本体の巡回の関数と手計算が一致し、
    /// 判定が全ての国に渡した欄は 1 つの値で（全ての国で同じ・ビット一致）、今の状態の W と同じ桁にある（判定は四半期の途中の状態を読むので完全一致は求めない）。
    /// ⚠壊し方＝和に自国を数えない・門の閉じた生産者を数える・財を一部に限る・国ごとに別の W を渡す → 赤。</summary>
    [Fact]
    public void 世界の合計Wは全ての国の全ての生産者の雇用の和と一致する()
    {
        var sim = BoardOn.Value;
        double want = HandWorld(sim);
        Assert.True(want > 0, "盤面に高度労働力の雇用が無い＝産出か投入の読みが死んでいる");
        double scan = Discovery.WorldAdvLabor(sim.Nations.Select(n => n.World), out long visited);
        Assert.True(System.Math.Abs(scan - want) <= 1e-9 * want, $"本体の巡回 {scan} ≠ 手計算 {want}");
        Assert.True(visited > 1000, $"訪れた生産者が少なすぎる（{visited}）");
        // 国が 1 つ欠けると W は小さくなる（全ての国を数える）
        double one = Discovery.WorldAdvLabor(sim.Nations.Skip(1).Select(n => n.World), out _);
        Assert.True(one < scan, "最初の国を除いても W が変わらない＝その国を数えていない");
        // 判定が渡した欄：全ての国で同じ値・今の状態の W と同じ桁
        double? first = null;
        foreach (var n in sim.Nations)
        {
            var got = n.World.LastDiscoveryView?.WorldAdvLabor;
            Assert.NotNull(got);
            first ??= got;
            Assert.Equal(first!.Value, got!.Value);
            Assert.True(got.Value > 0.5 * want && got.Value < 2.0 * want, $"判定の W {got} が今の状態の W {want} と桁が違う");
        }
    }

    /// <summary>d＞0 の行の国の技術力は 70×S×C に一致し（S は全ての国・行で共通の W から・C は自国の A だけ・押しを掛けない）、d≦0 の行は今の式（70×1×押し）。人口を読まない・技師の居ない国は技術水準が高くても 0。
    /// 実盤面の全ての国×非制度・非思想の行で、評価した技術力が 70×(手計算の S)×(手計算の C) と一致する（電動機の親は時計の形の定め）。S は手書きの区間線形（本体の ClockStage を使わない）。
    /// ⚠壊し方＝S に自国の A を使う（国ごとに時計が違う）・S に行の親の産業の W を使う（枝 dd の形）・C を 1 にする・押しを掛ける・A÷人口に変える・d≦0 の行にも押しを外す → 赤。</summary>
    [Fact]
    public void 技術力は70かけるSかけるCで_人口を読まず_産業の無い国は0()
    {
        var sim = BoardOn.Value;
        var c = sim.Coef;
        double w0 = TestClock.Value, wid = c.DiscoveryClockRampWidth;
        double wTot = HandWorld(sim);
        int compared = 0, zeroed = 0, mid = 0, d0 = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var view = w.LastDiscoveryView;
            Assert.NotNull(view?.WorldAdvLabor);
            var terms = Discovery.Evaluate(w, view! with { WorldAdvLabor = wTot }, w.Coef, includeKnown: true);   // 今の状態の W で評価する（保存された欄は判定の途中の状態の値）
            for (int row = 0; row < terms.Length; row++)
            {
                var d = Catalog.Discoveries[row];
                if (Discovery.IsSpread(d.Field)) continue;
                var rp = RepOf(w, row);
                if (rp is null) continue;
                if (Catalog.DiscoveryAfter[row] >= 0 && !Discovery.Known(w.Discovered, Catalog.DiscoveryAfter[row])) continue;   // 前提の行がまだの国は判定しない（点数 0）
                double dd = (d.Decade - 1780) / 10.0;
                if (!(dd > 0)) { d0++; continue; }   // d≦0 の行は下の別の検査
                double a = HandA(w, ParentGoods(row, rp));   // 電動機の親は今の規則（行の財か置き換える先の財＋電力の投入・旗 DiscoveryClockMotorParent は c132kx で消した）
                double sHand = System.Math.Clamp((wTot / (w0 * dd) - 1.0) / wid + 0.5, 0.0, 1.0);
                double cHand = a > 0 ? a / (a + TestOwn) : 0.0;
                double want = Discovery.TermMax * sHand * cHand;
                Assert.True(System.Math.Abs(terms[row].Tech - want) <= 1e-9 * System.Math.Max(1.0, want), $"{n.World.SelfPower?.BaseName} {d.Name}：技術力 {terms[row].Tech} ≠ 70×S×C＝{want}（A={a}・W={wTot}・S={sHand}）");
                compared++; if (a <= 0) { Assert.Equal(0.0, terms[row].Tech); zeroed++; }
                if (sHand > 0 && sHand < 1) mid++;
            }
        }
        Assert.True(compared > 500, $"比べた国×行が少なすぎる（{compared}）");
        Assert.True(zeroed > 20, $"技師の居ない産業の国が盤面に無い（{zeroed}）＝C＝0 の検査が空回りしている");
        Assert.True(mid > 20, $"傾斜の途中（0＜S＜1）の国×行が少なすぎる（{mid}）＝W0 の検査用の値が盤面の W と合っていない");
        Assert.True(d0 > 0, "d≦0 の行が盤面に無い");

        // 人口を読まない：英の人口を 10 倍にしても技術力は変わらない／技術水準を変えても変わらない（制度と思想の行を除く）
        var uk = Nation(sim, "イギリス");
        var before = Discovery.Evaluate(uk, uk.LastDiscoveryView, uk.Coef, includeKnown: true);
        double savedTech = uk.TechLevel;
        foreach (var rg in uk.Regions) foreach (var p in rg.Pops) p.Size *= 10;
        uk.TechLevel = 5.0;
        try
        {
            var after = Discovery.Evaluate(uk, uk.LastDiscoveryView, uk.Coef, includeKnown: true);
            for (int row = 0; row < before.Length; row++)
                if (!Discovery.IsSpread(Catalog.Discoveries[row].Field)) Assert.Equal(before[row].Tech, after[row].Tech);
        }
        finally { foreach (var rg in uk.Regions) foreach (var p in rg.Pops) p.Size /= 10; uk.TechLevel = savedTech; }
    }

    /// <summary>d≦0 の行（蒸気機関）は旗が正でも今の式のまま＝技術力 70×押し（押しが 1 未満なら 70 に届かない）。旗が 0 の盤面の同じ行と技術力が等しい（世界の合計も A も読まない）。
    /// ⚠壊し方＝d≦0 の行も 70×S×C に入れる（枝 dw の形＝押しを落とす・A＝0 の国で 0 になる）→ 赤。</summary>
    [Fact]
    public void d0以下の行は旗が正でも今の式_押しを掛ける()
    {
        int steam = Row("蒸気機関");
        Assert.True(Catalog.Discoveries[steam].Decade <= Catalog.DiscoveryBaseDecade);
        var on = BoardOn.Value; var off = BoardOff.Value;
        int compared = 0;
        foreach (var n in on.Nations)
        {
            var wOff = off.Nations.Find(m => m.World.SelfPower?.BaseName == n.World.SelfPower?.BaseName)?.World;
            if (wOff is null || RepOf(n.World, steam) is null) continue;
            var tOn = Discovery.Evaluate(n.World, n.World.LastDiscoveryView, n.World.Coef, includeKnown: true)[steam].Tech;
            var tOff = Discovery.Evaluate(wOff, wOff.LastDiscoveryView, wOff.Coef, includeKnown: true)[steam].Tech;
            Assert.True(System.Math.Abs(tOn - tOff) <= 1e-9 * System.Math.Max(1.0, tOff), $"{n.World.SelfPower?.BaseName}：蒸気機関の技術力が旗で変わった（{tOn} ≠ {tOff}）");
            compared++;
        }
        Assert.True(compared > 20, $"比べた国が少なすぎる（{compared}）");
        // 押しを掛けている確かめ：英の蒸気機関の投入（機械・石炭）が輸入に頼る盤面（自国の産出に対し輸入が圧倒的）にすると押し＜1＝技術力が 70 を下回る。同じ盤面で旗が正の側も旗 0 の側と等しい
        var ukOn = Nation(on, "イギリス"); var ukOff = Nation(off, "イギリス");
        var saved = new List<(Market M, double Imp)>();
        foreach (var w in new[] { ukOn, ukOff })
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                foreach (int g in new[] { Catalog.MachineryId, Catalog.CoalId })
                { var m = rg.Markets[g]; saved.Add((m, m.LastImport)); m.LastImport += 1e7; }
            }
        try
        {
            double pOn = Discovery.Evaluate(ukOn, ukOn.LastDiscoveryView, ukOn.Coef, includeKnown: true)[steam].Tech;
            double pOff = Discovery.Evaluate(ukOff, ukOff.LastDiscoveryView, ukOff.Coef, includeKnown: true)[steam].Tech;
            Assert.True(pOn < Discovery.TermMax - 1.0, $"輸入に頼る盤面でも蒸気機関の技術力が 70 のまま（{pOn}）＝押しを掛けていない");
            Assert.True(System.Math.Abs(pOn - pOff) <= 1e-9 * System.Math.Max(1.0, pOff), $"押しの掛かった技術力が旗で違う（{pOn} ≠ {pOff}）");
        }
        finally { foreach (var (m, imp) in saved) m.LastImport = imp; }
    }

    /// <summary>時計は暦を読まない（Q11）：①同じ国の同じ盤面で tick（暦の年）だけ 40 年ずらしても技術力は同じ ②同じ tick で世界の雇用 W だけ違えば（欄を変えて渡す）、傾斜の途中の行の技術力が違う。
    /// ⚠壊し方＝ClockStage が Year／四半期の添字を読む（①が赤）・W を読まない（②が赤）。</summary>
    [Fact]
    public void 時計は暦を読まず_世界の雇用だけで動く()
    {
        var sim = BoardOn.Value;
        var uk = Nation(sim, "イギリス");
        var view = uk.LastDiscoveryView!;
        double wTot = HandWorld(sim);
        var t0 = Discovery.Evaluate(uk, view with { WorldAdvLabor = wTot }, uk.Coef, includeKnown: true);
        long savedTick = uk.Tick; int savedYear = uk.Year;
        uk.Tick += Calendar.DaysPerYear * 40L; uk.Year += 40;   // 暦の年を 40 年ずらす（Tick は日・Year は別に持つ）
        try
        {
            var t1 = Discovery.Evaluate(uk, view with { WorldAdvLabor = wTot }, uk.Coef, includeKnown: true);
            for (int row = 0; row < t0.Length; row++)
                if (!Discovery.IsSpread(Catalog.Discoveries[row].Field)) Assert.Equal(t0[row].Tech, t1[row].Tech);
        }
        finally { uk.Tick = savedTick; uk.Year = savedYear; }
        // 同じ tick で W だけ 1.04 倍（d＝7 の行は S が 0.5 付近から 0.9 付近へ）
        var t2 = Discovery.Evaluate(uk, view with { WorldAdvLabor = wTot * 1.04 }, uk.Coef, includeKnown: true);
        int moved = 0;
        for (int row = 0; row < t0.Length; row++)
        {
            var d = Catalog.Discoveries[row];
            if (Discovery.IsSpread(d.Field) || d.Decade != Catalog.DiscoveryBaseDecade + 70 || !(t0[row].Tech > 0)) continue;
            Assert.True(t2[row].Tech > t0[row].Tech, $"{d.Name}：世界の雇用を 1.04 倍にしても技術力が増えない");
            moved++;
        }
        Assert.True(moved >= 1, $"確かめた行が少なすぎる（{moved}）");
    }

    /// <summary>既定（W0＝0）は (415) の式のまま＝世界の合計の欄を渡しても渡さなくても技術力が同じ（Am が正でも W0＝0 なら新しい経路に入らない）。電動機の親は旗が 0 なら今の規則。
    /// ⚠壊し方＝W0＝0 でも世界の合計を読む → 欄の有無で技術力が変わり赤。電動機の親を旗に関わらず蒸気機関の財にする → 電動機の A が変わり赤。</summary>
    [Fact]
    public void W0がゼロなら世界の合計を読まず_415の式のまま()
    {
        var sim = BoardOff.Value;
        int compared = 0, push1 = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var view = w.LastDiscoveryView;
            Assert.NotNull(view);
            Assert.Null(view!.WorldAdvLabor);
            var withW = Discovery.Evaluate(w, view with { WorldAdvLabor = 123456.0 }, w.Coef, includeKnown: true);
            var noW = Discovery.Evaluate(w, view, w.Coef, includeKnown: true);
            for (int row = 0; row < withW.Length; row++)
            {
                Assert.Equal(noW[row].Tech, withW[row].Tech);
                var d = Catalog.Discoveries[row];
                if (Discovery.IsSpread(d.Field)) continue;
                compared++;
                // (415) の式：技術力＝70×A÷(A＋A0×d)×押し（押し≦1）。押しが 1 の行（投入が全て自国）は等しい。⚠壊し方＝既定で新しい経路（70×S×C）に入る → 上限を超える行が出て赤
                var rp0 = RepOf(w, row);
                if (rp0 is null) continue;
                if (Catalog.DiscoveryAfter[row] >= 0 && !Discovery.Known(w.Discovered, Catalog.DiscoveryAfter[row])) continue;
                double aOld = HandA(w, ParentGoods(row, rp0)), dd = (d.Decade - 1780) / 10.0;
                double ef = !(dd > 0) ? 1.0 : !(aOld > 0) ? 0.0 : aOld / (aOld + w.Coef.DiscoveryEngineerHalfPerMonth * dd);   // 手書きの (415) の比（本体の関数を使わない）
                double old415 = Discovery.TermMax * ef;
                Assert.True(noW[row].Tech <= old415 * (1 + 1e-9) + 1e-9, $"{d.Name}：既定の技術力 {noW[row].Tech} が (415) の上限 {old415} を超えた");
                if (old415 > 0) Assert.True(noW[row].Tech > 0, $"{d.Name}：(415) の式で技師が居る行の既定の技術力が 0＝新しい経路（W0 を見ない）に入っている");
                if (old415 > 0 && System.Math.Abs(noW[row].Tech - old415) <= 1e-9 * System.Math.Max(1.0, old415)) { push1++; }
            }
        }
        Assert.True(compared > 500);
        Assert.True(push1 > 200, $"押しが 1 で (415) の式と一致した国×行が少なすぎる（{push1}）");

        // 電動機の親：今の規則（置き換える先の財か自分の財＋電力の投入）＝蒸気機関の財を足さない
        int motor = Row("電動機");
        var rp = RepOf(Nation(sim, "イギリス"), motor);
        Assert.NotNull(rp);
        var eng = new double[Catalog.Goods.Length];
        eng[Catalog.SteamEngineId] = 5.0; eng[Catalog.ElectricityId] = 2.0;
        Assert.Equal(2.0, Discovery.ParentEngineers(motor, rp!, eng));   // 蒸気機関の財（5.0）は足さない＝労働力の財を除いた投入（電力 2.0）だけ
        // 旗は W0 と幅の両方が正のときだけ：W0 が正でも幅が 0 なら新しい経路に入らない（巡回も走らない）
        var half = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        half.Coef.DiscoveryClockWorldPerMonth = 1000; half.Coef.DiscoveryClockRampWidth = 0;
        foreach (var n in half.Nations) { n.World.Coef.DiscoveryClockWorldPerMonth = 1000; n.World.Coef.DiscoveryClockRampWidth = 0; }
        half.StepQuarter();
        Assert.Null(Nation(half, "イギリス").LastDiscoveryView?.WorldAdvLabor);
        Assert.Equal(0, half.CostTotals()[CostLedger.DiscoveryWorldScans]);
    }

    /// <summary>旗が正のときだけ世界の合計の巡回が走り、台帳の行が増える：既定の構築で一度判定した世界の <c>LastDiscoveryView.WorldAdvLabor</c> は null（計器でない）・台帳の行は 0、旗が正なら非 null・台帳の行は正。
    /// ⚠壊し方＝旗に関わらず巡回する／台帳に数えない → 赤（計算量の宣言の外の費用）。</summary>
    [Fact]
    public void 世界の合計の巡回は旗が正のときだけ走り_台帳に数える()
    {
        Assert.Null(Nation(BoardOff.Value, "イギリス").LastDiscoveryView?.WorldAdvLabor);
        Assert.Equal(0, BoardOff.Value.CostTotals()[CostLedger.DiscoveryWorldScans]);
        Assert.NotNull(Nation(BoardOn.Value, "イギリス").LastDiscoveryView?.WorldAdvLabor);
        Assert.True(BoardOn.Value.CostTotals()[CostLedger.DiscoveryWorldScans] > 1000);
    }

    [Fact]
    public void 台帳の表の長さと番号が合う()
    {
        Assert.Equal(CostLedger.Count, CostLedger.Names.Length);
        Assert.Equal(CostLedger.Count, CostLedger.UnitMicros.Length);
        Assert.Equal(23, CostLedger.DiscoveryWorldScans);        // 重なりの読みの指定：dk＝23・lw＝24（22 は main の不満度を読む行）
        Assert.Equal(CostLedger.DiscoveryWorldScanUnitMicros, CostLedger.UnitMicros[CostLedger.DiscoveryWorldScans]);
        Assert.True(CostLedger.UnitMicros[CostLedger.DiscoveryWorldScans] > 0);
    }

    /// <summary>新しい既定（係数に何も書かない盤面）で、開幕の世界の高度労働力の雇用 W は d＝2 の行の傾斜の下端 W0×2×(1−w/2) より小さい＝d＞0 の行は 1783 年のうちに 1 本も開かない（時計が 1783 年の洪水を止める＝狙い T0・T1）。
    /// 期待値は手計算の不等式（W0 の既定は 7854.08・d の最小は 2・w は 0.1 → 下端 14,923）と、行ごとの解禁の有無（制度と思想の行・d≦0 の行・データで開幕に解禁済みの行は除く）。
    /// ⚠壊し方＝既定を W0＝0 に戻す（(415) の式へ → 転炉鋼・機関車が 1783 年に開く）・傾斜の下端を W0×d より下げる → 赤。</summary>
    [Fact]
    public void 新しい既定では1783年のうちにd正の行は1本も開かない()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        var before = sim.Nations.Select(n => n.World.Discovered is { } k ? (bool[])k.Clone() : new bool[Catalog.Discoveries.Length]).ToList();   // 開幕に解禁済みの行（データの既知の集合）
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        double wTot = HandWorld(sim);
        var c = sim.Coef;
        Assert.True(c.DiscoveryClockWorldPerMonth > 0 && c.DiscoveryOwnHalfPerMonth > 0, "新しい既定で時計の形が点いていない");
        Assert.True(wTot < c.DiscoveryClockWorldPerMonth * 2.0 * (1.0 - c.DiscoveryClockRampWidth / 2.0), $"開幕の W {wTot} が d＝2 の傾斜の下端以上＝この検査の前提が崩れた");
        int checkedRows = 0, opened = 0;
        for (int i = 0; i < before.Count; i++)   // 分離・建国で増えた国は数えない（開幕の国だけ）
        {
            var w = sim.Nations[i].World;
            for (int row = 0; row < Catalog.Discoveries.Length; row++)
            {
                var d = Catalog.Discoveries[row];
                if (Discovery.IsSpread(d.Field) || d.Decade <= Catalog.DiscoveryBaseDecade) continue;
                if (row < before[i].Length && before[i][row]) continue;
                checkedRows++;
                if (Discovery.Known(w.Discovered, row)) { opened++; }
            }
        }
        Assert.True(checkedRows > 1000, $"確かめた国×行が少なすぎる（{checkedRows}）");
        Assert.Equal(0, opened);
    }

    /// <summary>電動機の親の産業は今の規則（行の財か置き換える先の財の技師＋電力の投入）で、蒸気機関の財は足さない（旗 DiscoveryClockMotorParent は c132kx で消した）。
    /// 電動機の技術力は 70×S×C で、A は番人の側で生産者をたどって組む。⚠壊し方＝蒸気機関の財を常に足す → 本検査で赤（A蒸気≠A旧の国で技術力が合わない）。</summary>
    [Fact]
    public void 電動機の親の産業は蒸気機関の財を足さない今の規則()
    {
        int motor = Row("電動機");
        double dd = (Catalog.Discoveries[motor].Decade - 1780) / 10.0;
        int differ = 0, positive = 0, compared = 0;
        foreach (var (sim, flag) in new[] { (BoardMotorOff.Value, false) })
        {
            double w0 = TestClockMotor.Value, wid = sim.Coef.DiscoveryClockRampWidth;
            double wTot = HandWorld(sim);
            double sHand = System.Math.Clamp((wTot / (w0 * dd) - 1.0) / wid + 0.5, 0.0, 1.0);
            Assert.True(sHand > 0.2 && sHand < 0.8, $"電動機の S が傾斜の途中でない（{sHand}）＝検査が空回りする");
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                var rp = RepOf(w, motor);
                if (rp is null) continue;
                if (Catalog.DiscoveryAfter[motor] >= 0 && !Discovery.Known(w.Discovered, Catalog.DiscoveryAfter[motor])) continue;
                var terms = Discovery.Evaluate(w, w.LastDiscoveryView! with { WorldAdvLabor = wTot }, w.Coef, includeKnown: true);
                double aSteam = HandA(w, ParentGoods(motor, rp, extraParent: Catalog.SteamEngineId)), aOld = HandA(w, ParentGoods(motor, rp));
                double a = aOld;
                double want = Discovery.TermMax * sHand * (a > 0 ? a / (a + TestOwn) : 0.0);
                Assert.True(System.Math.Abs(terms[motor].Tech - want) <= 1e-9 * System.Math.Max(1.0, want), $"{n.World.SelfPower?.BaseName} 電動機（旗 {flag}）：技術力 {terms[motor].Tech} ≠ {want}（A蒸気={aSteam}・A旧={aOld}）");
                compared++; if (want > 0) positive++;
                if (aSteam != aOld) differ++;   // 蒸気機関の財を足すと A が変わる国（検査が空回りしていない印）
            }
        }
        Assert.True(compared >= 10, $"比べた国×旗が少なすぎる（{compared}）");
        // （旗 DiscoveryClockMotorParent の道が消え、電動機の A は電力の投入の技師だけ＝技術力が正の国は盤面に無いことがある。正の件数は門にしない）
        Assert.True(differ >= 3, $"蒸気機関の技師が電動機の A を変える国が少なすぎる（{differ}）＝旗の検査が空回りしている");
    }
}

/// <summary>★(461) 計器 clock_s（世界の合計・予備計測）：計器を出しても軌道は変わらず（出す／出さないの状態の全ビット一致）、W の行が正で最大の国以上・T の行の A が世界の合計以下・C の行が訪れた生産者を持つ。
/// 計器を静的な切り替え（<see cref="Instruments.On"/>）の上で走らせるため、他のどの試験とも並べない（<see cref="InstrumentsTests"/> と同じ集まり）。</summary>
[Collection("Instruments")]
public class DiscoveryClockInstrumentTests
{
    private static MultiNationSim Make()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
        return sim;
    }

    private static List<long> Digest(MultiNationSim sim)
    {
        var d = new List<long>();
        void A(double v) => d.Add(BitConverter.DoubleToInt64Bits(v));
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            A(w.Treasury.Value); A(w.TotalMoney.Value);
            foreach (var r in w.Regions) { A(r.SumPopulation()); foreach (var m in r.Markets) { A(m.Price); A(m.Stock); } foreach (var p in r.Producers) A(p.Output); }
            foreach (bool k in w.Discovered) d.Add(k ? 1 : 0);
        }
        return d;
    }

    private static void Run(MultiNationSim sim, int quarters, bool instr)
    {
        if (instr) Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        if (instr) Instruments.Phase(sim, 1789UL, "warm");
        try
        {
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (instr) Instruments.Quarter(sim, 1789UL, q); }
        }
        finally { if (instr) Instruments.Close(sim); }
    }

    [Fact]
    public void clock_sを出しても軌道は変わらず_Wの行は正で最大の国以上_Cの行は訪れた生産者を持つ()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_clock_" + Guid.NewGuid().ToString("N"));
        try
        {
            var off = Make(); Run(off, 17, instr: false);
            Instruments.Configure(dir, "*");
            try { var on = Make(); Run(on, 17, instr: true); Assert.Equal(Digest(off), Digest(on)); }
            finally { Instruments.Configure(null, null); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "clock_s1789.tsv"));
            var h = lines[0].Split('\t');
            Assert.Equal(["kind", "quarter", "year", "row", "decade", "worldTotalPerMonth", "holders", "ownNation", "ownPerMonth", "top1Nation", "top1PerMonth", "britainPerMonth", "francePerMonth", "prussiaPerMonth", "chinaPerMonth"], h);
            var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
            int iw = Array.IndexOf(h, "worldTotalPerMonth"), ib = Array.IndexOf(h, "britainPerMonth"), ifr = Array.IndexOf(h, "francePerMonth"), ipr = Array.IndexOf(h, "prussiaPerMonth"), icn = Array.IndexOf(h, "chinaPerMonth"), it1 = Array.IndexOf(h, "top1PerMonth"), ih = Array.IndexOf(h, "holders");
            double P(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            var wrows = rows.Where(r => r[0] == "W").ToList();
            Assert.True(wrows.Count >= 2, $"W の行が少なすぎる（{wrows.Count}）＝計器が働いていない");
            Assert.True(wrows.Any(r => P(r[iw]) > 0), "世界の合計が全ての標本で 0");
            foreach (var r in wrows) Assert.True(P(r[iw]) >= P(r[it1]) - 1e-9 * Math.Max(1.0, P(r[iw])), "世界の合計が最大の国より小さい");
            var crow = rows.Last(r => r[0] == "C");
            Assert.True(P(crow[ih]) > 0 && P(crow[iw]) >= 0, "C の行（巡回の累計）が空");
            var t = rows.Where(r => r[0] == "T").ToList();
            Assert.True(t.Count > 20, $"T の行が少なすぎる（{t.Count}）");
            foreach (var r in t)
            {
                double W = P(r[iw]), tol = 1e-9 * Math.Max(1.0, W);
                Assert.True(P(r[it1]) <= W + tol, $"{r[3]}：最大の国の A が世界の合計を超えた");   // 国の A（親の産業）≦ その国の全ての財の雇用 ≦ W
                Assert.True(P(r[ib]) + P(r[ifr]) + P(r[ipr]) + P(r[icn]) <= W + tol, $"{r[3]}：名指しの国の A の和が世界の合計を超えた");
            }
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
