using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★発見の作り替えの番人（段2A・2026-10-01・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §1A-2）。
/// 発見は2値・点数＝技術力（0〜70）＋必要性（0〜70）＋伝わり（0〜70）が一度でも100に届いたら解禁・失われない。
/// 作者 2026-09-30「中国では人と生糸が余っているので、紡績機は導入されない、という形にしたい。なので、それでよい」
/// 「労働を代替する発明は、安い人件費の国では必要とされない、という認識でよい」。
/// ★作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する。
/// 例えば蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」＝物を生む行の必要性は生み出す物（代替する物）の値段の高さ（(159)）。</summary>
public class DiscoveryScoreTests
{
    private static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == name);

    /// <summary>1四半期だけ回した盤面（市場の需要と値段が立つ・開幕の助走は通さない＝速い）。</summary>
    private static MultiNationSim Board(ulong seed = 7, Coefficients? coef = null)
    {
        var sim = coef is null ? new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent())
                               : new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent(), coef);
        sim.StepQuarter();
        return sim;
    }

    /// <summary>★(474) 見つける力の半分点 N0 に置く無限大＝産地の技術力 70×N÷(N＋N0) が厳密に 0（旧の盤面の技術水準 0 の代わり・今の機構の係数）。</summary>
    private const double NoFindingPower = double.PositiveInfinity;

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    [Fact]
    public void 点数の性質_どの項も1つでは届かずどの2つでも届く()
    {
        Assert.True(Discovery.TermMax < Discovery.GateScore, "1つの項だけで門に届く");
        Assert.True(2 * Discovery.TermMax >= Discovery.GateScore, "2つの項が揃っても門に届かない");
        // 盤面の上でも3項はどれも [0, 70]
        var sim = Board();
        foreach (var n in sim.Nations)
            foreach (var t in Discovery.Evaluate(n.World, n.World.LastDiscoveryView, n.World.Coef, includeKnown: true))
                foreach (double v in new[] { t.Tech, t.Need, t.Transmit })
                    Assert.True(v >= 0 && v <= Discovery.TermMax, $"{n.World.SelfPower?.Name}：項が範囲の外（{v}）");
    }

    [Fact]
    public void 難易度は年代を1本のYで写す_1780年代は0()
    {
        var c = new Coefficients();
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
        {
            var d = Catalog.Discoveries[r];
            Assert.Equal(c.DiscoveryTechPerDecade * (d.Decade - Catalog.DiscoveryBaseDecade) / 10.0, Discovery.Difficulty(r, c), 12);
            Assert.True(d.Decade % 10 == 0 && d.Decade >= Catalog.DiscoveryBaseDecade, $"{d.Name}：年代が10年刻みでない（{d.Decade}）");
        }
        Assert.Equal(0.0, Discovery.Difficulty(Row("蒸気機関"), c));
    }

    /// <summary>今の四半期に国内の市場が生きている財か（エンジンの判定と同じ：需要と、産出＋純輸入の正の分がある）。</summary>
    private static bool AliveIn(World w, int g)
    {
        double d = 0, p = 0, im = 0;
        foreach (var rg in w.Regions) if (rg.State == RegionState.Active) { d += rg.Markets[g].LastDemand; p += rg.Markets[g].LastProduced; im += rg.Markets[g].LastImport - rg.Markets[g].LastExport; }
        return d > 1e-9 && p + (im > 0 ? im : 0) > 1e-9;
    }

    /// <summary>行の代表の生産者（その国にある同じ財・同じレシピの最初の生産者）。</summary>
    private static Producer RepOf(World w, int row)
        => w.Regions.SelectMany(rg => rg.Producers).First(p => p.DiscoveryRow == row);

    /// <summary>番人の側で組んだ「新しいレシピで作る費用」＝自己費用×自己費用の物価水準÷物価水準＋Σ投入の量×差し替えの値段（投入が全て国内で生きている盤面だけ）。
    /// ★(169) 製法・動力・機関車の行が同じ規則（動力のレシピも自己費用を数える＝AI の推奨・作者の承認は未確認・(172) 反証役 vd3#3・vd3#8）。</summary>
    private static double CostByHand(World w, Producer rep, double[] px)
    {
        double c = rep.MarginalCost * w.CostPriceLevel / w.NominalOf(1.0);
        foreach (var inp in rep.Inputs) if (inp.QtyPerOutput > 0) c += inp.QtyPerOutput * px[inp.GoodId];
        return c;
    }

    /// <summary>★作者 2026-10-02 の番人（(159)→(165)→(169)）＝「蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」＋比べる相手（作者「2でよい」＝代替する物の値段÷その発見が出す物の値段）
    /// ＝蒸気機関の行の必要性の強度＝ln（動力〔労働力の財〕の平滑化した値段÷蒸気の動力を新しいレシピで作る費用〔★(169) 他の行と同じ規則＝自己費用＋蒸気機関の財と石炭の平滑化した値段×量〕）：
    /// 同じ盤面・同じ技術で、動力が蒸気の動力の費用と同じか安い国は必要性0＝自力では発見しない（技術力だけでは門に届かない）・2倍なら ln 2・
    /// 石炭を2倍にすると費用が上がって必要性が下がる（参照価格ではなく今の費用と比べる）・人件費（1人の所得）は読まない・自己費用の分だけ費用が高い（数えない形より必要性が低い）。
    /// ⚠壊し方＝参照価格と比べる・自己費用（人の働き）を数えない（(165)〜(167) の形）・人件費を読む・技術力だけで解禁する（門を70へ）→ 赤。
/// ★(172) 反証役 vd3#3：作者の言葉は一般則（「動力が高いか」）と比べる相手（「2でよい」）まで。自己費用（人の働き）を数える部分は AI の推奨（(169)・作者の承認は未確認＝作者への確認①）＝壊し方の「数えない形」を赤にするのは AI の推奨を守る部分（作者の答えで数えない形に決まれば、この部分の主張を作者の形へ書き直す）。</summary>
    [Fact]
    public void 蒸気機関の行は他の行と同じ規則で自己費用と石炭と機械の動力の費用と比べる_人件費は読まない()
    {
        var sim = Board();
        int steam = Row("蒸気機関"), g = Catalog.LaborId;
        World? w = null;
        foreach (var n in sim.Nations)
        {
            var wn = n.World;
            if (wn.DiscoveryPrice is null) continue;
            var rep0 = wn.Regions.SelectMany(rg => rg.Producers).FirstOrDefault(p => p.DiscoveryRow == steam);
            if (rep0 is null || !AliveIn(wn, g) || !AliveIn(wn, Catalog.CoalId)) continue;
            if (Discovery.Evaluate(wn, null, wn.Coef, includeKnown: true)[steam].Tech > 69.9) { w = wn; break; }   // 動力と石炭の市場が生きていて技術力が最大の国
        }
        Assert.NotNull(w);
        w!.Discovered[steam] = false;   // 盤面の最初の四半期で解禁済みでも、判定の1本を確かめるために戻す（点数は解禁の有無に依らない）
        var rep = RepOf(w, steam);
        var own = (double[])w.DiscoveryPrice!.Clone();
        for (int i = 0; i < own.Length; i++) if (!(own[i] >= 0)) own[i] = 1.0;   // 盤面の観測なしを埋める
        own[Catalog.SteamEngineId] = 1e-3;   // 蒸気機関の財＝国内の市場が死んでいれば世界の値段（下の view）で読む＝どちらでも同じ値（作る費用より安い）
        var view = new Discovery.WorldView(null, null, Discovery.WorldMarket.Flat(own));   // 世界の平滑化した値段＝同じ値段（市場の死んだ投入を同じ値で読む・運賃と関税の無い市場）
        double cost = CostByHand(w, rep, own);
        double selfPart = rep.MarginalCost * w.CostPriceLevel / w.NominalOf(1.0);
        Assert.True(selfPart > 0.1 * cost, $"前提が崩れた（蒸気の動力のレシピの自己費用 {selfPart}・費用 {cost}）");
        Assert.True(cost > 0 && rep.Inputs.Any(i => i.GoodId == Catalog.CoalId) && rep.Inputs.Any(i => i.GoodId == Catalog.SteamEngineId), "前提が崩れた（蒸気の動力のレシピ）");
        Discovery.Terms At(double overCost, double coalMul = 1.0)
        {
            var px = (double[])own.Clone(); px[Catalog.CoalId] *= coalMul;
            px[g] = CostByHand(w, rep, px) * overCost;
            return Discovery.Evaluate(w, view with { Market = Discovery.WorldMarket.Flat(px) }, w.Coef, includeKnown: true, realPrice: px)[steam];
        }
        var same = At(1.0); var cheap = At(0.5); var dear = At(2.0);
        Assert.Equal(0.0, same.Need);
        Assert.Equal(0.0, cheap.Need);
        Assert.True(cheap.Score < Discovery.GateScore, $"動力が安い国が技術力だけで届く（{cheap.Score:F1}）");
        Assert.Equal(Discovery.TermMax * System.Math.Log(2.0) / (System.Math.Log(2.0) + w.Coef.DiscoveryNeedHalfProcess), dear.Need, 9);   // 強度＝ln(動力÷自己費用と石炭と機械の費用)
        // ★(169) 自己費用を数える＝動力が「石炭と機械だけ」の費用の2倍でも、自己費用を足した費用を超えなければ必要性0（数えない形なら ln 2）
        {
            var pxN = (double[])own.Clone(); pxN[g] = 2.0 * (cost - selfPart);
            double needN = Discovery.Evaluate(w, view with { Market = Discovery.WorldMarket.Flat(pxN) }, w.Coef, includeKnown: true, realPrice: pxN)[steam].Need;
            double sN = System.Math.Log(2.0 * (cost - selfPart) / cost);
            Assert.Equal(sN > 0 ? Discovery.TermMax * sN / (sN + w.Coef.DiscoveryNeedHalfProcess) : 0, needN, 9);
            Assert.True(needN < Discovery.TermMax * System.Math.Log(2.0) / (System.Math.Log(2.0) + w.Coef.DiscoveryNeedHalfProcess) - 1e-9, "動力の行が自己費用を数えていない（(165)〜(167) の形）");
        }
        // 今の費用と比べる＝動力の値段は同じで石炭だけ2倍＝費用が上がり必要性が下がる
        var px2 = (double[])own.Clone(); px2[g] = 2.0 * cost;
        var pxC = (double[])px2.Clone(); pxC[Catalog.CoalId] *= 2.0;
        double needBase = Discovery.Evaluate(w, view with { Market = Discovery.WorldMarket.Flat(px2) }, w.Coef, includeKnown: true, realPrice: px2)[steam].Need, needCoal = Discovery.Evaluate(w, view with { Market = Discovery.WorldMarket.Flat(pxC) }, w.Coef, includeKnown: true, realPrice: pxC)[steam].Need;
        Assert.Equal(Discovery.TermMax * System.Math.Log(2.0) / (System.Math.Log(2.0) + w.Coef.DiscoveryNeedHalfProcess), needBase, 9);
        double s2 = System.Math.Log(2.0 * cost / CostByHand(w, rep, pxC));
        Assert.Equal(s2 > 0 ? Discovery.TermMax * s2 / (s2 + w.Coef.DiscoveryNeedHalfProcess) : 0, needCoal, 9);
        Assert.True(needCoal < needBase, "石炭が高くなっても必要性が下がらない（今の費用と比べていない）");
        Assert.True(dear.Tech == cheap.Tech, "動力の値段で技術力が変わった");
        // 人件費（働き手の所得）は読まない＝所得を10倍にしても必要性は同じ
        foreach (var rg in w.Regions) foreach (var p in rg.Pops) p.EarnedPerMonth *= 10;
        Assert.Equal(dear.Need, At(2.0).Need);
        // 判定の1本（Step）も同じ＝安い国は解禁しない・高い国は解禁する（判定は平滑化した値段を読む＝その値を置く）
        var keep = w.DiscoveryPrice!;
        var pxCheap = (double[])own.Clone(); pxCheap[g] = 0.5 * cost;
        w.DiscoveryPrice = pxCheap;
        Discovery.Step(w, view with { Market = Discovery.WorldMarket.Flat(pxCheap) }, w.Coef);
        Assert.False(w.Discovered[steam]);
        if (dear.Score >= Discovery.GateScore)
        {
            var pxDear = (double[])own.Clone(); pxDear[g] = 2.0 * cost;
            w.DiscoveryPrice = pxDear;
            Discovery.Step(w, view with { Market = Discovery.WorldMarket.Flat(pxDear) }, w.Coef);
            Assert.True(w.Discovered[steam]);
        }
        w.DiscoveryPrice = keep;
    }

    /// <summary>★(169) 開幕の既知の発見はデータ（world.json の knownDiscoveries＝NationConfig.KnownDiscoveries）＝1783年より前に史実でその国が使っていた発見：
    /// ①データの名前は全て発見の表にある ②構築の直後（助走の前）から英は蒸気機関と特許を持ち、データに無い国は1つも持たない（ヴェネツィアは特許だけ）
    /// ③助走（StartWarmup）は点数で開幕の既知を足さない＝助走の後も同じ集合（判定は最初の四半期の終わりから）。
    /// ⚠壊し方＝データを読まない・助走の終わりに点数で判定する（旧 段2A〜(167) の形）→ 赤。</summary>
    [Fact]
    public void 開幕の既知の発見はデータで_助走は点数で足さない()
    {
        foreach (var cfg in WorldData.Nations())
            foreach (var name in cfg.KnownDiscoveries)
                Assert.True(Catalog.DiscoveryRowNamed(name) >= 0, $"{cfg.Name} の knownDiscoveries「{name}」が発見の表に無い");
        var uk = WorldData.Nations().First(n => n.Name == "イギリス");
        Assert.Contains("蒸気機関", uk.KnownDiscoveries);
        var sim = new MultiNationSim(WorldData.Nations(), 3, _ => new UtilityAgent());
        int steam = Discovery.SteamRow, patent = Discovery.PatentRow;
        void Check(string when)
        {
            foreach (var n in sim.Nations)
            {
                var w = n.World; string name = w.SelfPower?.BaseName ?? "";
                var cfg = WorldData.Nations().First(c => c.Name == name);
                for (int r = 0; r < Catalog.Discoveries.Length; r++)
                    Assert.True(w.Discovered[r] == cfg.KnownDiscoveries.Contains(Catalog.Discoveries[r].Name),   // ★(415) 合成染料の行は旗 DiscoveryEngineerHalfPerMonth が正（既定）なら開幕で解禁しない＝データどおり
                        $"{when}：{name} の {Catalog.Discoveries[r].Name} がデータと違う（{w.Discovered[r]}）");
            }
        }
        Check("構築の直後");
        Assert.True(Nation(sim, "イギリス").Discovered[steam] && Nation(sim, "イギリス").Discovered[patent]);
        Assert.False(Nation(sim, "フランス").Discovered[steam]);
        sim.StartWarmup();
        Check("助走の後");
    }

    /// <summary>★(169) 1780年代の蒸気機関＝「鋳鉄の機関」（時代のレシピ・前期の軍需と同じ型）：蒸気機関の財の既定のレシピがある全ての州に並び、
    /// 投入は鉄1.832＋銅0.414＋石炭0.4（＋労働力と高度労働力）＝鋼と機械を使わない・参照価格の世界で既定のレシピと同じ費用（1% 以内）・
    /// 門は転炉鋼の発見までは開き、発見で退く（退場の表）。⚠壊し方＝足さない・鋼か機械を投入にする・退場させない → 赤。</summary>
    [Fact]
    public void 一七八〇年代の蒸気機関は鋳鉄の機関で作れ_転炉鋼の発見で退く()
    {
        var sim = Board();
        var w = Nation(sim, "フランス");
        int bessemer = Row("転炉鋼");
        int withDefault = 0;
        foreach (var rg in w.Regions)
        {
            var def = rg.Producers.FirstOrDefault(p => p.GoodId == Catalog.SteamEngineId && p.RecipeName == "");
            if (def is null) continue;
            withDefault++;
            var ci = rg.Producers.SingleOrDefault(p => p.GoodId == Catalog.SteamEngineId && p.RecipeName == "鋳鉄の機関");
            Assert.NotNull(ci);
            Assert.DoesNotContain(ci!.Inputs, i => i.GoodId == Catalog.SteelId || i.GoodId == Catalog.MachineryId);
            Assert.Equal(1.832, ci.Inputs.Single(i => i.GoodId == Catalog.IronId).QtyPerOutput, 12);
            Assert.Equal(0.414, ci.Inputs.Single(i => i.GoodId == Catalog.CopperId).QtyPerOutput, 12);
            Assert.Equal(0.4, ci.Inputs.Single(i => i.GoodId == Catalog.CoalId).QtyPerOutput, 12);
            // 参照価格の世界の費用＝自己費用＋Σ投入×参照価格（労働力と高度労働力も参照価格）＝既定のレシピと同じ（1% 以内）
            double RefCost(Producer p) => p.MarginalCost + p.Inputs.Sum(i => i.QtyPerOutput * Catalog.RefPrices[i.GoodId]);
            Assert.Equal(1.0, RefCost(ci) / RefCost(def), 2);
            Assert.Equal(bessemer, ci.RetiredByRow);
            Assert.False(w.Discovered[bessemer], "前提が崩れた（開幕に転炉鋼）");
            Assert.Equal(1.0, Discovery.Gate(rg, ci));
            w.Discovered[bessemer] = true;
            Assert.Equal(0.0, Discovery.Gate(rg, ci));   // 転炉鋼の発見で退く
            w.Discovered[bessemer] = false;
        }
        Assert.True(withDefault > 0, "前提が崩れた（仏に蒸気機関の財の既定のレシピが無い）");
    }

    /// <summary>伝わりだけでは届かない・技術力と揃えば届く（設計 §1A-2 の性質）＝必要性の無い行（合成ゴム＝ゴムをまだ作れない国の製法は必要性0）で、
    /// ①時計 S が 0（世界の高度労働力の雇用 W＝0）・接触は最大（伝わり≈70）→ 届かない ②時計 S が 1（W が極めて大きい）＋同じ接触 → 技術力は 70×C（自国の項 C＝親の産業の技師 A÷(A＋Am)）で、
    /// 自国の項 C を最大（Am を極小・親の産業に技師が居れば C≈1）にした国は届く。技術力の前提は技術水準でなく今の機構の W（<see cref="Discovery.WorldView.WorldAdvLabor"/>）と C で作る（★(474) 旧の盤面〔旗0〕への固定を外した）。
    /// ⚠壊し方＝門を70へ（伝わりだけで届く）→ ①が赤。</summary>
    [Fact]
    public void 伝わりだけでは届かず技術力と揃えば届く()
    {
        var sim = Board();
        int row = Row("窒素固定");   // ★(493) 合成ゴムは化学薬品でなく電力（開幕に技師が居ない）を材料にした＝親の産業の技師が居る別の工程の行（ハーバーボッシュ＝硫酸＋電力）で同じ性質を見る
        var contact = new double[Catalog.Discoveries.Length];
        contact[row] = 1e6;   // 接触は最大（伝わり→70）
        var viewLow = new Discovery.WorldView(contact, WorldAdvLabor: 0);          // 時計 S＝0
        var viewHigh = new Discovery.WorldView(contact, WorldAdvLabor: DiscoveryPremise.OpenClockW);   // 時計 S＝1
        int tried = 0, reached = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Discovered[row] || !(Discovery.Evaluate(w, null, w.Coef)[row].Need == 0)) continue;
            tried++;
            var only = Discovery.Evaluate(w, viewLow, w.Coef)[row];
            Assert.True(only.Transmit > 69.9 && only.Tech == 0 && only.Need == 0, $"前提が崩れた（{only}）");
            Discovery.Step(w, viewLow, w.Coef);
            Assert.False(w.Discovered[row], $"{w.SelfPower?.Name}：伝わりだけで解禁した");
            w.Coef.DiscoveryOwnHalfPerMonth = DiscoveryPremise.FullOwnShareAm;   // 自国の項 C≈1（既定の Am では開幕の技師が少なく C≈1e-4）
            var both = Discovery.Evaluate(w, viewHigh, w.Coef)[row];
            if (!(both.Score >= Discovery.GateScore)) continue;
            reached++;
            Discovery.Step(w, viewHigh, w.Coef);
            Assert.True(w.Discovered[row], $"{w.SelfPower?.Name}：技術力と伝わりが揃っても解禁しない（{both}）");
        }
        Assert.True(tried > 10 && reached > 0, $"盤面が空回り（試した国 {tried}・届いた国 {reached}）");
    }

    /// <summary>解禁は失われない（2値の知識＝作者裁定 §1A-2）：点数が0の行を解禁済みにしておけば、判定を何度通しても解禁のまま。</summary>
    [Fact]
    public void 解禁は失われない()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        int row = Row("ラジオ");
        Assert.True(Discovery.Evaluate(w, DiscoveryPremise.ClosedClock(), w.Coef, includeKnown: true)[row].Score < Discovery.GateScore, "前提が崩れた");   // ★(474) 旧は w.TechLevel＝0（時計 S＝0 が今の機構の技術力 0）
        w.Discovered[row] = true;
        for (int i = 0; i < 3; i++) Discovery.Step(w, null, w.Coef);
        Assert.True(w.Discovered[row]);
    }

    /// <summary>財の解禁＝その財のレシピの解禁の最大（設計 §1-2）：地理差のレシピ（表に無い）が1本でもある財は常に解禁・既定のレシピの行しかない財は発見まで0。
    /// 需要は世界の解禁の和で読む（どこかの国が作れる財は輸入で手に入る）＝国の解禁が無くても世界が解禁していれば需要の門は開く・作る門は閉じたまま。</summary>
    [Fact]
    public void 財の解禁はレシピの最大_需要は世界の解禁の和()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        var r = w.Region;
        int ng = Catalog.Goods.Length;
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.PaperId));   // 紙＝ぼろ布製紙（地理差）がある＝木材パルプを発見していなくても解禁
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.SteelId));   // 鋼＝坩堝鋼（地理差）
        int canned = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, ""), fish = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, "魚の缶詰");
        Assert.False(w.Discovered[canned] || w.Discovered[fish], "前提が崩れた（缶詰が発見済み）");
        Assert.Equal(0.0, Discovery.GoodGate(r, Catalog.CannedFoodId));

        var union = new bool[Catalog.Discoveries.Length];
        union[canned] = true;   // 世界のどこかが缶詰を発見した
        var prod = new double[ng]; var dem = new double[ng];
        Discovery.GoodGates(r, prod, ng, r.Discovered);
        Discovery.GoodGates(r, dem, ng, union);
        Assert.Equal(0.0, prod[Catalog.CannedFoodId]);   // 作る門＝自国の解禁
        Assert.Equal(1.0, dem[Catalog.CannedFoodId]);    // 需要の門＝世界の解禁の和
        for (int g = 0; g < ng; g++) Assert.Equal(Discovery.GoodGate(r, g), prod[g]);   // 1回の走査と財ごとの読みは同じ値
    }

    /// <summary>需要の門は世界の解禁の和を読む＝EconomyStep の消費の1本の番人（双子の盤面・片方だけ世界の和に缶詰を立てる→缶詰の需要が立つ）。
    /// ⚠壊し方＝需要の門を自国の解禁で読む（goodGate）→ 双子の需要が同じ＝赤。</summary>
    [Fact]
    public void 需要の門は世界の解禁の和で開く_自国が未発見でも輸入の需要が立つ()
    {
        var a = Board(); var b = Board();
        int canned = Catalog.DiscoveryRowOf(Catalog.CannedFoodId, "");
        int i = a.Nations.FindIndex(n => !n.World.Discovered[canned]);
        Assert.True(i >= 0);
        var wa = a.Nations[i].World; var wb = b.Nations[i].World;
        Assert.NotNull(wa.WorldDiscovered);
        Assert.False(wa.WorldDiscovered![canned] || wb.WorldDiscovered![canned], "前提が崩れた（世界が缶詰を発見済み）");
        wb.WorldDiscovered![canned] = true;   // B だけ世界の和に缶詰（全ての国で同じ配列＝B の世界全体）
        EconomyStep.Run(wa.Region, wa.Coef, 1.0, wa.Bank, wa);
        EconomyStep.Run(wb.Region, wb.Coef, 1.0, wb.Bank, wb);
        Assert.True(wb.Region.Markets[Catalog.CannedFoodId].LastDemand > wa.Region.Markets[Catalog.CannedFoodId].LastDemand,
            $"世界が発見した缶詰に需要が立たない（A {wa.Region.Markets[Catalog.CannedFoodId].LastDemand}／B {wb.Region.Markets[Catalog.CannedFoodId].LastDemand}）");
        Assert.Equal(0.0, Discovery.GoodGate(wb.Region, Catalog.CannedFoodId));   // 作る門は閉じたまま
    }

    /// <summary>★物を生む行の比べる相手の規則（(165)・作者 2026-10-02「その発見が生み出すものが代替するものが高いか、で判定する」＋「2でよい」＝代替する物の値段÷その発見が出す物の値段）＝
    /// 必要性の強度＝ln（代替する物の平滑化した値段×換算の比÷新しいレシピで作る費用〔自己費用×自己費用の物価水準÷物価水準＋Σ投入×平滑化した値段〕）・1以下は0：
    /// ①製法（人絹＝絹織物）＝絹が人絹の費用の2倍なら ln 2・費用と同じなら0・関係の無い財（馬）を高くしても動かない・投入（製材）を高くすると下がる＝参照価格ではなく今の費用と比べる
    /// ②新製品（ガス灯＝灯油）＝換算の比（新しい財1単位が置き換える代替する物の量＝普通の値段で同じ値打ち＝ガスの参照価格÷灯油の参照価格）を掛けた灯油の値段がガスの費用の2倍なら ln 2
    /// ③機関車＝陸の輸送の値段÷鉄道の輸送の費用（運転の費用＝清算の陸路の費用の鉄道÷馬＋★(169) 線路と機関車の資本の費用÷馬の運賃・内陸の州の潜在量で重み付け）④代替する物の需要が国内に無いと0
    /// ⑤陸の輸送の値段＝(1÷通した割合)^(1/2ε)・通した割合0 は財の値段の天井（PriceCeilFactor）。
    /// ⚠壊し方＝参照価格と比べる（(159) の形）・代替する物の欄を読まない・換算の比を掛けない・投入の値段を読まない・機関車の比べる相手を1にする・機関車の資本の費用を読まない（★(169)）・
    ///   陸の輸送を使っていない国の機関車を運転の費用で読む（★(169)）・需要の無い財を読む → 赤。
    /// ⚠(167) 反証役の指摘で名前を直した（旧「…_参照価格は読まない」）＝製法・動力・機関車は参照価格を読まないが、新製品は換算の比（参照価格の比）を掛ける
    ///   ＝強度＝ln((代替する物の値段÷その参照価格)÷(新しいレシピの費用÷新しい財の参照価格))＝新製品の必要性の0点は参照価格の比が決める（作者への確認②）。
    ///   ★(169) 物の単位の換算の腕は入れていない（電力・送電網・冷蔵・ゴム・自動車がどの国でも見つからない＝実装ログ (169)・作者への確認）。</summary>
    [Fact]
    public void 代替する物の値段は新しいレシピで作る費用と比べる_新製品だけ参照価格の比で単位を換える()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        Assert.NotNull(w.DiscoveryPrice);
        int ng = Catalog.Goods.Length;
        Assert.Equal(ng + 1, w.DiscoveryPrice!.Length);   // 末尾＝陸の輸送の値段
        var basePx = (double[])w.DiscoveryPrice.Clone();
        for (int i = 0; i < ng; i++) if (!(basePx[i] >= 0)) basePx[i] = Catalog.RefPrices[i];   // 盤面の観測なしを埋める（下の view で世界の値段にも同じ値）
        basePx[ng] = 1.0;
        double[] Needs(double[] px) => Discovery.Evaluate(w, new Discovery.WorldView(null, null, Discovery.WorldMarket.Flat(px)), w.Coef, includeKnown: true, realPrice: px).Select(x => x.Need).ToArray();
        double NeedOf(int row, double s) => s > 0 ? Discovery.TermMax * s / (s + Discovery.NeedHalf(Catalog.Discoveries[row].Field, w.Coef)) : 0;
        // ①製法＝人絹（代替する物＝絹織物そのもの）
        int silk = Row("人絹"), gs = Catalog.SilkClothId;
        var repS = RepOf(w, silk);
        Assert.True(AliveIn(w, gs) && repS.Inputs.All(i => AliveIn(w, i.GoodId)), "前提が崩れた（英の絹織物か人絹の投入の市場）");
        double cS = CostByHand(w, repS, basePx);
        var px = (double[])basePx.Clone(); px[gs] = 2.0 * cS;
        Assert.Equal(NeedOf(silk, System.Math.Log(2.0)), Needs(px)[silk], 9);
        px[gs] = cS * (1.0 - 1e-9);
        Assert.Equal(0.0, Needs(px)[silk]);
        var other = (double[])basePx.Clone(); other[gs] = 2.0 * cS; other[Catalog.HorseId] *= 5.0;   // 関係の無い財（馬）だけ高い
        Assert.Equal(NeedOf(silk, System.Math.Log(2.0)), Needs(other)[silk], 9);
        var dearIn = (double[])basePx.Clone(); dearIn[gs] = 2.0 * cS; dearIn[Catalog.TimberId] *= 3.0;   // 投入（木）が高い＝費用が上がる（★(493) 人絹のパルプは製材でなく木から）
        Assert.True(repS.Inputs.Any(i => i.GoodId == Catalog.TimberId), "前提が崩れた（人絹の投入に木が無い）");
        Assert.Equal(NeedOf(silk, System.Math.Log(2.0 * cS / CostByHand(w, repS, dearIn))), Needs(dearIn)[silk], 9);
        // ②新製品＝ガス灯（代替する物＝灯油・換算の比＝ガスの参照価格÷灯油の参照価格）
        int gas = Row("ガス灯");
        Assert.Equal(Catalog.LampOilId, Catalog.Discoveries[gas].Replaces);
        double k = Catalog.RefPrices[Catalog.GasId] / Catalog.RefPrices[Catalog.LampOilId];
        Assert.Equal(k, Discovery.ReplacedUnits[gas], 12);
        Assert.Equal(1.0, Discovery.ReplacedUnits[silk]);
        var repG = RepOf(w, gas);
        Assert.True(AliveIn(w, Catalog.LampOilId) && repG.Inputs.All(i => AliveIn(w, i.GoodId)), "前提が崩れた（英の灯油かガスの投入の市場）");
        double cG = CostByHand(w, repG, basePx);
        var pg = (double[])basePx.Clone(); pg[Catalog.LampOilId] = 2.0 * cG / k;
        Assert.Equal(NeedOf(gas, System.Math.Log(2.0)), Needs(pg)[gas], 9);
        // ③機関車＝陸の輸送の値段÷鉄道の輸送の費用（内陸の州の潜在量で重み付けた 鉄道÷馬 の陸路の運転の費用＋★(169) 線路と機関車の資本の費用÷馬の陸の運賃）
        int loco = Row("機関車");
        Assert.Equal(Catalog.LandFreight, Catalog.Discoveries[loco].Replaces);
        double e = 1.0 / (2.0 * w.Coef.PriceElasticity);
        var cc = w.Coef;
        var repL = RepOf(w, loco);
        // 線路と機関車の投入の市場を英で生かす（値段の帳が与えた値段をそのまま読む盤面＝死んだ市場の着値と作る費用の安い方の規則は別の番人）
        var rgA = w.Regions.First(r => r.State == RegionState.Active);
        foreach (int g0 in repL.Inputs.Where(i => i.QtyPerOutput > 0).Select(i => i.GoodId).Append(Catalog.RailId))
        { rgA.Markets[g0].LastDemand = System.Math.Max(1.0, rgA.Markets[g0].LastDemand); rgA.Markets[g0].LastProduced = System.Math.Max(1.0, rgA.Markets[g0].LastProduced); }
        double RailPx(double[] px0) => px0[Catalog.RailId];
        double pRail = RailPx(basePx);
        double pLoco = CostByHand(w, repL, basePx);
        Assert.True(AliveIn(w, Catalog.RailId) && repL.Inputs.All(i => !(i.QtyPerOutput > 0) || AliveIn(w, i.GoodId)), "前提が崩れた（英の線路か機関車の投入の市場）");
        double capital = cc.TransportDecay * (pRail + pLoco) / (2.0 * cc.TransportCapPerUnit);
        double horseUnit = cc.TransportMargin * cc.LandSeaCostRatio / 10_000.0;   // 運賃革命の前（単独の判定＝市場が無いので1）
        int inland = 0; double pot = 0, sum = 0, sumRun = 0, f = cc.RailCostFraction;
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active || rg.IsPort) continue;
            inland++; rg.LastTradePotential = 1.0 + inland; rg.LastTradeThrottle = 0.25;   // 内陸の州の潜在量と律速率を置く（通した割合 0.25）
            int ti = (int)rg.Terrain;
            bool water = (rg.Terrain == Terrain.WaterMajor && cc.TerrainWaterMajorCost > 0) || (rg.Terrain == Terrain.WaterMinor && cc.TerrainWaterMinorCost > 0);
            double m0 = water ? (rg.Terrain == Terrain.WaterMajor ? cc.TerrainWaterMajorCost : cc.TerrainWaterMinorCost) : cc.TerrainHorseCost[ti];
            double hm = horseUnit * m0 * rg.PortLandKm;
            if (!(hm > 0)) continue;
            double kk = water ? f : f * cc.TerrainRailCost[ti] / cc.TerrainHorseCost[ti];
            pot += rg.LastTradePotential; sum += rg.LastTradePotential * (kk + capital / hm); sumRun += rg.LastTradePotential * kk;
        }
        Assert.True(inland > 0 && pot > 0, "前提が崩れた（英に陸の脚のある内陸の州が無い）");
        double rail = sum / pot;
        Assert.True(rail > sumRun / pot && double.IsFinite(rail), $"前提が崩れた（鉄道の輸送の費用 {rail}・運転だけ {sumRun / pot}）");
        // 資本の費用を読む＝線路の値段を上げると鉄道の輸送の費用が上がり必要性が下がる（陸の運賃は同じ）
        {
            var pr = (double[])basePx.Clone(); pr[ng] = 2.0 * rail;
            foreach (var inp in repL.Inputs) if (inp.QtyPerOutput > 0 && inp.GoodId != Catalog.LaborId && inp.GoodId != Catalog.AdvLaborId) pr[inp.GoodId] *= 20.0;   // 機関車の資本財の投入を高くする
            double pLocoDear = CostByHand(w, repL, pr);
            double railDear = (sum - sumRun) / pot * (cc.TransportDecay * (RailPx(pr) + pLocoDear) / (2.0 * cc.TransportCapPerUnit)) / capital + sumRun / pot;
            double sD = System.Math.Log(2.0 * rail / railDear);
            Assert.Equal(sD > 0 ? NeedOf(loco, sD) : 0, Needs(pr)[loco], 9);
            Assert.True(Needs(pr)[loco] < NeedOf(loco, System.Math.Log(2.0)) - 1e-9, "機関車の比べる相手が線路と機関車の資本の値段を読まない");
        }
        var pl = (double[])basePx.Clone(); pl[ng] = 2.0 * rail;
        Assert.Equal(NeedOf(loco, System.Math.Log(2.0)), Needs(pl)[loco], 9);
        pl[ng] = rail * 0.5;   // 鉄道の方が高い＝不足なし
        Assert.Equal(0.0, Needs(pl)[loco]);
        // ★(169) 陸の輸送を使っていない国（内陸の州の潜在の交易が0）は必要性0＝代替する物の需要が無ければ0（他の行と同じ規則・旧＝運転の費用だけ＝島国も必要性 54）
        {
            var keepPot = w.Regions.Select(rg => rg.LastTradePotential).ToArray();
            foreach (var rg in w.Regions) if (rg.State == RegionState.Active && !rg.IsPort) rg.LastTradePotential = 0;
            var pz = (double[])basePx.Clone(); pz[ng] = 100.0;   // 陸の運賃がどれほど高くても
            Assert.Equal(0.0, Needs(pz)[loco]);
            for (int i = 0; i < w.Regions.Count; i++) w.Regions[i].LastTradePotential = keepPot[i];
        }
        // ⑤陸の輸送の値段＝(1÷通した割合)^(1/2ε)・通した割合0 は天井（財の値段の安全網と同じ）
        //   ★(166) 天井は漸近形（MarketFlow.SoftBandLn＝内側はほぼ恒等＝値段 4 で相対 1.2e-5 だけ下がる）＝下の「陸の運賃は天井へ漸近し超えない」の番人
        Assert.Equal(System.Math.Pow(1.0 / 0.25, e), Discovery.InstantRealPrices(w)[ng], 3);
        foreach (var rg in w.Regions) if (rg.State == RegionState.Active && !rg.IsPort) rg.LastTradeThrottle = 0;
        Assert.Equal(w.Coef.PriceCeilFactor, Discovery.InstantRealPrices(w)[ng], 12);
        // ④代替する物の需要が国内に無い＝値段が費用の2倍でも0
        foreach (var rg in w.Regions) rg.Markets[gs].LastDemand = 0;
        var pd = (double[])basePx.Clone(); pd[gs] = 2.0 * cS;
        Assert.Equal(0.0, Needs(pd)[silk]);
    }

    /// <summary>★(165)→(169) その国に市場の値段が無い財（作らず輸入もしない＝今の四半期に国内の市場が死んでいる）の読み方（理由で決めた・推奨で進めた）＝手に入れる安い方
    /// ＝着値（★(169) 売り手の平滑化した値段×(1＋運賃＋関税)＝運賃＝TransportMargin×(1＋DistanceMarginScale×距離÷1万km)×運賃革命・関税＝買い手の関税の楔）とこの国で門の開いたレシピで作る費用の小さい方・
    /// どちらも無ければ費用が立たない＝0：盤面＝英の人絹（投入の製材の市場を英で殺す・売り手＝仏）→ ①着値が作る費用より安ければ着値で費用を組む ②高ければ英の製材のレシピで作る費用
    /// ③死んだ市場の凍った値段は読まない ④取引できない投入（電力＝電動機）は世界の値段では読まない ⑤遠い売り手ほど・関税が高いほど着値が高い。
    /// ⚠壊し方＝死んだ市場の凍った値段を読む・世界の値段を読まない・作る費用を読まない・取引できない財を世界の値段で読む・運賃か関税を足さない（(165)〜(167) の形）→ 赤。</summary>
    [Fact]
    public void 市場の値段が無い財は着値か作る費用の安い方で読む_着値は運賃と関税を足す()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        int ng = Catalog.Goods.Length, silk = Row("人絹"), gs = Catalog.SilkClothId, gl = Catalog.TimberId;   // ★(493) 人絹のパルプは製材でなく木から（G14）
        var px = (double[])w.DiscoveryPrice!.Clone();
        for (int i = 0; i < ng; i++) if (!(px[i] >= 0)) px[i] = Catalog.RefPrices[i];
        var repS = RepOf(w, silk);
        double cS = CostByHand(w, repS, px);
        px[gs] = 4.0 * cS;   // 絹は十分に高い＝必要性が立つ
        // 製材のレシピ（英の州の門の開いた製材の生産者・名前ごと）の作る費用＝殺す前の投入の値段で
        var lumberRecipes = w.Regions.Where(rg => rg.State == RegionState.Active).SelectMany(rg => rg.Producers.Where(p => p.GoodId == gl && Discovery.Gate(rg, p) > 0))
            .GroupBy(p => p.RecipeName).Select(gp => gp.First()).ToList();
        Assert.True(lumberRecipes.Count > 0 && lumberRecipes.All(p => p.Inputs.All(i => AliveIn(w, i.GoodId))), "前提が崩れた（製材のレシピの投入）");
        double make = lumberRecipes.Min(p => CostByHand(w, p, px));
        // 製材の国内の市場を殺す（需要0＝観測なし）＝自国の値段は読まない
        foreach (var rg in w.Regions) { rg.Markets[gl].LastDemand = 0; rg.Markets[gl].LastProduced = 0; rg.Markets[gl].LastImport = 0; }
        Assert.False(AliveIn(w, gl));
        double qL = repS.Inputs.First(i => i.GoodId == gl).QtyPerOutput;
        // 売り手＝仏（製材の市場が生きている1国・重み1）・買い手＝英（市場の死んだ国＝世界の市場に加わらない）
        var fr = Nation(sim, "フランス");
        var cw = w.Coef;
        double km = Geo.DistanceKm(w.Latitude, w.Longitude, fr.Latitude, fr.Longitude);
        Assert.True(km > 100, $"前提が崩れた（英仏の距離 {km}）");
        double freight = 0.8;   // 運賃革命の倍率（任意の値を渡す）
        double duty = System.Math.Max(0, InternationalMarket.TariffRate(w, cw));
        double LandedFactor(double d) => 1.0 + cw.TransportMargin * (1.0 + d / 10_000.0) * freight + duty;
        Discovery.WorldMarket MarketOf(double wl)
        {
            var sp = Enumerable.Repeat(Discovery.NoPrice, ng + 1).ToArray(); sp[gl] = wl;
            fr.DiscoveryPrice = sp;
            var xa = Enumerable.Repeat(Discovery.NoPrice, ng + 1).ToArray(); var xb = (double[])xa.Clone(); xb[gl] = wl;
            var one = Enumerable.Repeat(1.0, ng).ToArray();
            return Discovery.WorldPrices([w, fr], [(xa, one), (xb, one)], freight);
        }
        double NeedAt(double? worldLumber)
        {
            var p = (double[])px.Clone(); p[gl] = 1e6;   // 死んだ市場の凍った値段（読まれない）
            var mk = worldLumber is { } wl ? MarketOf(wl) : null;
            return Discovery.Evaluate(w, new Discovery.WorldView(null, null, mk), cw, includeKnown: true, realPrice: p)[silk].Need;
        }
        double Expect(double lumberPrice)
        {
            double c = cS - qL * px[gl] + qL * lumberPrice;
            double s = System.Math.Log(4.0 * cS / c);
            return s > 0 ? Discovery.TermMax * s / (s + w.Coef.DiscoveryNeedHalfProcess) : 0;
        }
        double fac = LandedFactor(km);
        Assert.True(fac > 1.0 + 1e-6, $"前提が崩れた（着値の倍率 {fac}）");
        Assert.Equal(Expect(make), NeedAt(null), 9);                              // 世界の市場が無い＝作る費用
        Assert.Equal(Expect(0.5 * make), NeedAt(0.5 * make / fac), 9);            // 着値が安い＝着値（売り手の値段×運賃と関税の倍率）
        Assert.Equal(Expect(make), NeedAt(10 * make), 9);                         // 着値が高い＝作る費用
        Assert.True(NeedAt(0.5 * make) < NeedAt(0.5 * make / fac) - 1e-9, "着値に運賃と関税を足していない（(165)〜(167) の形）");
        // ⑤遠い売り手ほど着値が高い＝売り手の位置を遠くへ（経済の中心の座標を置き直す）
        {
            double lat0 = fr.Latitude, lon0 = fr.Longitude;
            fr.Latitude = -lat0; fr.Longitude = lon0 + 120.0;
            double kmFar = Geo.DistanceKm(w.Latitude, w.Longitude, fr.Latitude, fr.Longitude);
            Assert.True(kmFar > km + 1000, $"前提が崩れた（遠い売り手 {kmFar}）");
            double v = 0.3 * make / LandedFactor(kmFar);
            Assert.Equal(Expect(0.3 * make), NeedAt(v), 9);
            fr.Latitude = lat0; fr.Longitude = lon0;
        }
        // ④取引できない投入（電力＝電動機）は世界の値段では読まない＝英に電力の市場も門の開いた電力のレシピも無ければ費用が立たない＝0
        int motor = Row("電動機");
        Assert.True(Catalog.Goods[Catalog.ElectricityId].NonTradeable, "前提が崩れた（電力は取引できない財）");
        Assert.True(!AliveIn(w, Catalog.ElectricityId) && !w.Regions.Any(rg => rg.Producers.Any(p => p.GoodId == Catalog.ElectricityId && Discovery.Gate(rg, p) > 0)),
            "前提が崩れた（開幕の英に電力がある）");
        var p2 = (double[])px.Clone(); p2[Catalog.LaborId] = 100.0;          // 動力がどれほど高くても
        var wp2 = (double[])px.Clone(); wp2[Catalog.ElectricityId] = 1e-6;   // 世界の電力がどれほど安くても
        Assert.Equal(0.0, Discovery.Evaluate(w, new Discovery.WorldView(null, null, Discovery.WorldMarket.Flat(wp2)), w.Coef, includeKnown: true, realPrice: p2)[motor].Need);
    }

    /// <summary>★(167) 反証役の指摘（旧 (164) の低）＝値段の読み方の②（国内の市場が今の四半期に生きている財＝自国の平滑化した値段・他国の値段は読まない）の番人：
    /// 盤面＝英の人絹（代替する物の絹織物と人絹の投入の市場が英で生きている・絹は人絹の費用の2倍＝必要性が立つ）→ 世界の平滑化した値段だけを
    /// 代替する物と投入について 0.1倍・10倍に変えても必要性は同じ（世界の値段が無いときとも同じ）。
    /// ⚠壊し方＝生きた財でも世界の値段（またはその安い方）を読む → 赤。</summary>
    [Fact]
    public void 国内の市場が生きている財は自国の平滑化した値段を読み世界の値段では動かない()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        int ng = Catalog.Goods.Length, silk = Row("人絹"), gs = Catalog.SilkClothId;
        var px = (double[])w.DiscoveryPrice!.Clone();
        for (int i = 0; i < ng; i++) if (!(px[i] >= 0)) px[i] = Catalog.RefPrices[i];
        px[ng] = 1.0;
        var repS = RepOf(w, silk);
        Assert.True(AliveIn(w, gs) && repS.Inputs.All(i => AliveIn(w, i.GoodId)), "前提が崩れた（英の絹織物か人絹の投入の市場）");
        double cS = CostByHand(w, repS, px);
        px[gs] = 2.0 * cS;
        double NeedWith(double[]? wp) => Discovery.Evaluate(w, new Discovery.WorldView(null, null, wp is null ? null : Discovery.WorldMarket.Flat(wp)), w.Coef, includeKnown: true, realPrice: px)[silk].Need;
        double baseNeed = NeedWith(null);
        Assert.True(baseNeed > 0, $"前提が崩れた（人絹の必要性 {baseNeed}）");
        var cheap = (double[])px.Clone(); var dear = (double[])px.Clone();
        foreach (int g in repS.Inputs.Where(i => i.QtyPerOutput > 0).Select(i => i.GoodId).Append(gs)) { cheap[g] = 0.1 * px[g]; dear[g] = 10.0 * px[g]; }
        Assert.Equal(baseNeed, NeedWith(cheap), 12);
        Assert.Equal(baseNeed, NeedWith(dear), 12);
    }

    /// <summary>★(166) 陸の運賃（機関車の必要性の入力）の天井は財の値段と同じ漸近形＝内側はほぼ 1÷通した割合（通した割合 0.5 で 2）・天井（PriceCeilFactor）へ漸近して超えない・
    /// 通した割合が下がるほど高い（単調）・全く通らない（0）は天井。⚠壊し方＝硬い上限（小さい方を取る＝(165) の形）へ戻す → 通した割合 0.1%（1÷通した割合＝1000）が天井ちょうどになって赤。</summary>
    [Fact]
    public void 陸の運賃は天井へ漸近し超えない_内側は通した割合の逆数()
    {
        var sim = Board();
        var w = sim.Nations.Select(n => n.World).First(x => x.Regions.Count(r => r.State == RegionState.Active && !r.IsPort) >= 2);
        double cap = w.Coef.PriceCeilFactor;
        double Price(double served)
        {
            foreach (var r in w.Regions) if (r.State == RegionState.Active && !r.IsPort) { r.LastTradePotential = 1.0; r.LastTradeThrottle = served; }
            Assert.Equal(served, Discovery.LandFreightServed(w), 12);
            return Discovery.LandFreightPrice(w);
        }
        Assert.Equal(2.0, Price(0.5), 6);           // 内側＝1÷通した割合（PriceElasticity 0.5）
        Assert.Equal(1.0, Price(1.0), 9);
        double p1 = Price(0.01), p2 = Price(0.001), p3 = Price(1e-6);
        Assert.True(p1 < p2 && p2 < p3, $"通した割合が下がっても陸の運賃が上がらない（{p1}・{p2}・{p3}）");
        Assert.True(p3 < cap && p2 < 0.99 * cap, $"天井に張り付いた（硬い上限の形）＝{p2}・{p3}（天井 {cap}）");
        Assert.True(p2 > 0.9 * cap, $"天井へ漸近しない（{p2}）");
        Assert.Equal(cap, Price(0.0), 9);            // 全く通らない＝天井（漸近の極限）
    }

    /// <summary>★(165)→(169) 世界の平滑化した値段＝その四半期に国内の市場が生きている国（瞬間の値段の観測がある国）の平滑化した値段を、渡した重み（★(169) 判定の段はその国の産出＝売っている量）で重み付けた平均・
    /// どの国にも市場が無い財は観測なし・重み0の国（産出の無い国）は入らない。⚠壊し方＝市場の死んだ国の値段を入れる・重みで平均しない → 赤。呼び手が産出を渡すことは次の番人。</summary>
    [Fact]
    public void 世界の平滑化した値段は市場が生きている国の重みで平均し重み0の国は入らない()
    {
        int ng = Catalog.Goods.Length;
        var sim = Board();
        var a = sim.Nations[0].World; var b = sim.Nations[1].World;
        a.DiscoveryPrice = Enumerable.Repeat(2.0, ng + 1).ToArray();
        b.DiscoveryPrice = Enumerable.Repeat(5.0, ng + 1).ToArray();
        var xa = Enumerable.Repeat(1.0, ng + 1).ToArray(); var xb = Enumerable.Repeat(1.0, ng + 1).ToArray();
        var da = Enumerable.Repeat(1.0, ng).ToArray(); var db = Enumerable.Repeat(3.0, ng).ToArray();
        xb[7] = Discovery.NoPrice;   // b の財7 は市場が死んでいる
        xa[9] = Discovery.NoPrice; xb[9] = Discovery.NoPrice;   // 財9 はどの国にも市場が無い
        db[11] = 0;   // b は財11 を売っていない（産出0）
        var wp = Discovery.WorldPrices([a, b], [(xa, da), (xb, db)]).Average;
        Assert.Equal((1.0 * 2.0 + 3.0 * 5.0) / 4.0, wp[0], 12);
        Assert.Equal(2.0, wp[7], 12);
        Assert.Equal(Discovery.NoPrice, wp[9]);
        Assert.Equal(2.0, wp[11], 12);
    }

    /// <summary>呼び手の側の番人（(165)→(169)）＝多国の判定の段は世界の市場を判定へ渡す（画面と計器も同じ値＝LastDiscoveryView）＝四半期の後の平均は全ての国の平滑化した値段と瞬間の値段と
    /// ★(169) 産出（売っている量＝重み）から求めた値と同じ・運賃革命の倍率は清算と同じ1本（前線＝全ての国の技術水準の最大）。
    /// ⚠壊し方＝共有層が世界の市場を渡さない・重みを需要に戻す（(165)〜(167) の形）・運賃革命の倍率を渡さない → 赤。</summary>
    [Fact]
    public void 判定の段は世界の平滑化した値段を判定へ渡す()
    {
        var sim = Board();
        sim.StepQuarter();
        var view = sim.Nations[0].World.LastDiscoveryView;
        Assert.NotNull(view?.Market);
        var worlds = sim.Nations.Select(n => n.World).ToArray();
        var obs = worlds.Select(w =>
        {
            var x = Discovery.InstantRealPrices(w);
            var dem = new double[Catalog.Goods.Length];
            foreach (var rg in w.Regions) if (rg.State == RegionState.Active) for (int g = 0; g < dem.Length; g++) dem[g] += rg.Markets[g].LastProduced;   // ★(169) 重み＝産出
            return (x, dem);
        }).ToArray();
        var expect = Discovery.WorldPrices(worlds, obs).Average;
        Assert.Equal(expect.Length, view!.Market!.Average.Length);
        for (int g = 0; g < expect.Length; g++) Assert.Equal(expect[g], view.Market.Average[g], 9);
        Assert.Equal(1.0, view.Market.Freight, 12);
        // (843) 旗 EraOff の焼き込みで運賃革命の倍率は常に 1（倍率が 1 でない盤面の配線の確かめは旗 0 の枝とともに消した）
        Assert.Contains(expect, v => v >= 0);
    }

    /// <summary>伝わりの運び手＝その行の財（取引できるとき）＋レシピの投入の資本財（設計 §1A-7「財そのものの輸入で学ぶ」）。
    /// 労働を代替する発明（蒸気機関）は労働力を運べない＝蒸気機関の財で運ばれる。⚠壊し方＝資本財の投入を運び手にしない→赤。</summary>
    [Fact]
    public void 伝わりの運び手は財と資本財の投入()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        var carriers = Discovery.CarriersByRow(sim.Nations.Select(n => n.World));
        var steam = carriers[Row("蒸気機関")];
        Assert.Contains(Catalog.SteamEngineId, steam);
        Assert.DoesNotContain(Catalog.LaborId, steam);
        Assert.Contains(Catalog.SteelId, carriers[Row("転炉鋼")]);
        Assert.Contains(Catalog.MachineGunId, carriers[Row("機関銃")]);
        var byCarrier = Discovery.RowsByCarrier(carriers);
        Assert.Contains(Row("蒸気機関"), byCarrier[Catalog.SteamEngineId]);
    }

    // ===== ★新兵器の必要性の損耗の項（2026-10-01 (153)・設計 §1A-4 (4a)「その分野の脅威比＋交戦中のその分野の損耗の率」・作者裁定 2026-09-28 ⑤「陸・海・空で分ける」）=====

    /// <summary>新兵器の必要性は交戦中の**その分野の**損耗で上がり、他の分野の損耗では動かない（陸＝機関銃・戦車・化学兵器／海＝装甲艦・弩級戦艦／空＝軍用機）。
    /// 単位＝損耗の強さ（最大を1とした割合）は脅威の割合と同じ物差し＝強度は2つの和（換算の数を置かない）：損耗 x の必要性＝70×(脅威の割合＋x)÷(…＋半分点)。
    /// ⚠壊し方＝損耗を読まない・分野を取り違える（海の行が陸の損耗を読む）・損耗に換算の数を掛ける → 赤。記帳の側（戦死・艦隊摩耗）は WarThemeTests。</summary>
    [Fact]
    public void 新兵器の必要性は交戦中のその分野の損耗で上がり他の分野では動かない()
    {
        var sim = Board();
        int[] landRows = [Row("機関銃"), Row("戦車"), Row("化学兵器")], seaRows = [Row("装甲艦"), Row("弩級戦艦")];
        int airRow = Row("軍用機");
        int[] all = [.. landRows, .. seaRows, airRow];
        World? w = null;
        foreach (var n in sim.Nations)
        {
            // ★(344) 系統×段階の点火（DiscoveryTierGate＝1）の後は、前提の行（戦車・軍用機←内燃機関・弩級戦艦←装甲艦）がまだの国でこれらの行は判定されない（点数・項は 0）
            //   ＝この試験は損耗の項だけを見る＝前提の行を解禁済みにしてから読む（1四半期の盤面では内燃機関を知る国が無い）。
            foreach (int r in all) { int a = Catalog.DiscoveryAfter[r]; if (a >= 0) n.World.Discovered[a] = true; }
            // ★(474) 代表の生産者が居る行＝損耗を与えたとき必要性が立つ行（旧は技術力が正の行〔技術水準の押し〕＝今の機構では技術力は時計と自国の項で決まり、この試験の主題の損耗の項とは別）
            var t = Discovery.Evaluate(n.World, new Discovery.WorldView(null, Loss: Enumerable.Repeat(0.5, Discovery.LossDomains).ToArray()), n.World.Coef, includeKnown: true);
            if (all.All(r => t[r].Need > 0)) { w = n.World; break; }   // 6行とも代表の生産者が居る国
        }
        Assert.NotNull(w);
        double half = w!.Coef.DiscoveryNeedHalfWeapon;
        Discovery.Terms[] T(double land, double sea, double air)
        {
            var loss = new double[Discovery.LossDomains];
            loss[(int)WarDomain.Land] = land; loss[(int)WarDomain.Sea] = sea; loss[(int)WarDomain.Air] = air;
            return Discovery.Evaluate(w, new Discovery.WorldView(null, Loss: loss), w.Coef, includeKnown: true);
        }
        var t0 = T(0, 0, 0); var tl = T(0.5, 0, 0); var ts = T(0, 0.5, 0); var ta = T(0, 0, 0.5);
        var none = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (int r in all) Assert.Equal(none[r].Need, t0[r].Need);   // 損耗0＝損耗の無い判定と同じ
        static double StrengthOf(double need, double h) => h * need / (Discovery.TermMax - need);   // 必要性→強度（70×s÷(s＋h) の逆）
        static double NeedOf(double s, double h) => Discovery.TermMax * s / (s + h);
        void Check(int[] rows, Discovery.Terms[] hit, Discovery.Terms[] miss1, Discovery.Terms[] miss2, string dom)
        {
            foreach (int r in rows)
            {
                string nm = Catalog.Discoveries[r].Name;
                Assert.True(hit[r].Need > t0[r].Need, $"{nm}：{dom}の損耗で必要性が上がらない（{t0[r].Need:F3}→{hit[r].Need:F3}）");
                Assert.Equal(t0[r].Need, miss1[r].Need);
                Assert.Equal(t0[r].Need, miss2[r].Need);
                Assert.Equal(NeedOf(StrengthOf(t0[r].Need, half) + 0.5, half), hit[r].Need, 9);   // 強度＝脅威の割合＋損耗の強さ（同じ単位＝換算の数なし）
                Assert.True(hit[r].Tech == t0[r].Tech && hit[r].Transmit == t0[r].Transmit, $"{nm}：損耗で技術力か伝わりが動いた");
            }
        }
        Check(landRows, tl, ts, ta, "陸");
        Check(seaRows, ts, tl, ta, "海");
        Check([airRow], ta, tl, ts, "空");
    }

    /// <summary>★(194) 陸の新兵器の必要性の脅威の割合＝陸の必要量÷(陸の必要量＋自国の陸軍の大きさ〔ArmyShape.LandPotential＝兵の頭数×質×(1−海軍シェア)〕)＝陸の必要量（相手の陸軍を同じ1本で読む）と同じ物差し
    /// （(192) 反証役 vn1#1 の推奨で陸の必要量を即応度なしへ移した＝自国の側も移す・推奨で進めた＝作者の承認は未確認）。盤面＝1四半期の世界で機関銃の行の技術力が立つ国。
    /// ①陸の必要量＝自国の陸軍の大きさ＝脅威の割合 0.5（必要性から強度へ戻した値） ②自国の軍の総合ゲージ・即応度・熟練（症状）を変えても必要性は1ビットも動かない。
    /// ⚠突然変異「自国の LandPower を読む（旧）」で赤。</summary>
    [Fact]
    public void 陸の新兵器の必要性は自国の陸軍の大きさと比べ即応度を読まない()
    {
        var sim = Board();
        int mg = Row("機関銃");
        World? w = null;
        foreach (var n in sim.Nations)
        {
            var t = Discovery.Evaluate(n.World, new Discovery.WorldView(null, Loss: Enumerable.Repeat(0.5, Discovery.LossDomains).ToArray()), n.World.Coef, includeKnown: true);   // ★(474) 代表の生産者が居る行＝損耗を与えたとき必要性が立つ行（旧は技術力が正の行）
            if (t[mg].Need > 0 && n.World.SelfPower is { } sp0 && ArmyShape.LandPotential(sp0) > 0) { w = n.World; break; }
        }
        Assert.NotNull(w);
        var sp = w!.SelfPower!;
        double half = w.Coef.DiscoveryNeedHalfWeapon;
        sp.NeedLand = ArmyShape.LandPotential(sp);
        var a = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(0.5, half * a[mg].Need / (Discovery.TermMax - a[mg].Need), 9);   // 必要性→強度（70×s÷(s＋h) の逆）＝脅威の割合
        sp.Military *= 0.01; w.MilitaryReadiness *= 0.01; sp.ArmySkillMul = 2.0;   // 症状だけを動かす
        var b = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(a[mg].Need, b[mg].Need);
    }

    /// <summary>損耗の記帳（World.DiscoveryLossQ＝月ごとに積む）は判定で四半期の月の平均になり（画面と計器が同じ値を読む＝World.LastDiscoveryView.Loss）、記帳は捨てられる。
    /// 多国の世界でも四半期をまたいで積み残さない（判定の Discovery.Step が捨てる＝共有層と助走の捨てる行はこの番人では見ていない＝(154) の反証役の指摘・今は助走中に外交が止まり記帳が積まれない）。⚠壊し方＝捨てない（次の四半期へ積み残す）・平均にしない（月の和のまま）→ 赤。</summary>
    [Fact]
    public void 損耗の記帳は判定で四半期の月の平均になり捨てられる()
    {
        var sim = Board();
        var w = sim.Nations[0].World;
        var lq = new double[Discovery.LossDomains];
        lq[(int)WarDomain.Land] = 0.3; lq[(int)WarDomain.Sea] = 0.6; lq[(int)WarDomain.Air] = 0.9;
        w.DiscoveryLossQ = lq;
        Discovery.Step(w, new Discovery.WorldView(null), w.Coef);
        Assert.Null(w.DiscoveryLossQ);
        var l = w.LastDiscoveryView?.Loss;
        Assert.NotNull(l);
        Assert.Equal(0.3 / Calendar.MonthsPerQuarter, l![(int)WarDomain.Land], 12);
        Assert.Equal(0.6 / Calendar.MonthsPerQuarter, l[(int)WarDomain.Sea], 12);
        Assert.Equal(0.9 / Calendar.MonthsPerQuarter, l[(int)WarDomain.Air], 12);
        // 多国の四半期でも積み残さない（判定が捨てる・共有層の捨てる行はこの番人では見ていない＝(154)）
        foreach (var n in sim.Nations) n.World.DiscoveryLossQ = new double[Discovery.LossDomains];
        sim.StepQuarter();
        Assert.All(sim.Nations, n => Assert.Null(n.World.DiscoveryLossQ));
    }
    // ===== ★産地の発見（2026-10-01 (148)・Catalog.Sites＝大鉱床と植林）の番人 =====

    /// <summary>産地の生産者を探す（州の名前＝表の場所・財＝表の財）。</summary>
    private static (World W, Region R, Producer P) Site(MultiNationSim sim, string siteName)
    {
        int row = System.Array.FindIndex(Catalog.Sites, x => x.Name == siteName);
        Assert.True(row >= 0, $"表に {siteName} が無い");
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                foreach (var p in r.Producers)
                    if (p.SiteRow == row) return (n.World, r, p);
        throw new Xunit.Sdk.XunitException($"{siteName} の生産者が盤面に無い");
    }

    /// <summary>産地は表からだけ来る（州も植民地も同じ表・場所の名前で引く）：産地の生産者は表の場所の州にあり財が一致する・開始の盤面に場所がある行は
    /// ちょうど1つの生産者を持つ・銀の行は SilverRushEnabled＝0 の間は置かない（検討 Q13 (d)）・抽象の標的（アルタ=カリフォルニア・南アフリカ）は入植するまで無い。
    /// ⚠壊し方＝銀の旗を読まない → 西部の銀の生産者が産地になる＝赤。</summary>
    [Fact]
    public void 産地は表からだけ来る_銀は旗が0の間は置かない()
    {
        var sim = Board();
        var count = new int[Catalog.Sites.Length];
        var places = new HashSet<string>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                places.Add(r.Name);
                foreach (var p in r.Producers)
                {
                    if (p.SiteRow < 0) continue;
                    var st = Catalog.Sites[p.SiteRow];
                    Assert.Equal(st.Place, r.Name);
                    Assert.Equal(st.Good, p.GoodId);
                    Assert.True(p.IsRgo, $"{st.Name}：産地の生産者が採取でない");
                    count[p.SiteRow]++;
                }
            }
        Assert.Equal(0.0, new Coefficients().SilverRushEnabled);
        for (int i = 0; i < Catalog.Sites.Length; i++)
        {
            var st = Catalog.Sites[i];
            int expect = st.Good == Catalog.SilverId || !places.Contains(st.Place) ? 0 : 1;
            Assert.True(count[i] == expect, $"{st.Name}（{st.Place}）：産地の生産者 {count[i]}（期待 {expect}）");
        }
        foreach (var name in new[] { "ジャワのキナ", "ニルギリのキナ", "セイロンのキナ", "ルイジアナの硫黄", "九州の硫黄", "ヴィクトリアの金", "クロンダイクの金" })
            Assert.True(count[System.Array.FindIndex(Catalog.Sites, x => x.Name == name)] == 1, $"{name} が開始の盤面に無い");
    }

    /// <summary>★(171) 盤面の道具：その国の稼働中の州で財 g の市場を生かし（需要と産出を1）、瞬間と平滑化した実質の値段を <paramref name="real"/> に置く。</summary>
    private static void SiteMarket(World w, int g, double real)
    {
        double pl = w.NominalOf(1.0);
        foreach (var rr in w.Regions)
        {
            if (rr.State != RegionState.Active) continue;
            var m = rr.Markets[g]; m.Price = real * pl; m.LastDemand = 1; m.LastProduced = 1;
        }
        Assert.NotNull(w.DiscoveryPrice);
        w.DiscoveryPrice![g] = real;
    }

    /// <summary>★(171) 産地の2項の部品（人の数・その財の値段・その国で今のレシピで掘る費用）＝エンジンの関数が書く値。</summary>
    private static (Discovery.Terms T, double People, double V, double Cost) SiteParts(World w, Region r, Producer p, double[]? realPrice = null, Discovery.WorldView? view = null)
    {
        var parts = new double[3];
        var t = Discovery.SiteTerms(w, r, p, w.Coef, realPrice, view, parts);
        return (t, parts[0], parts[1], parts[2]);
    }

    /// <summary>見つかるまで産まない・見つかったら開く・失われない（2値）＋キナはアンデスだけが開始から（(129) の申し送り）：
    /// ①アジアのキナ（バタヴィア）の生産者は門0・ペルーの樹皮は門1 ②見つける力が0では届かない（技術力0＋必要性≦70）
    /// ③見つける力を戻し、樹皮の値段を掘る費用の e³ 倍に置くと届いて開く（門1）④見つける力を0へ戻して判定を重ねても開いたまま。
    /// ★(474) 産地の技術力は今の機構では人の厚み N÷(N＋N0) だけ（技術水準を読まない・(415)）＝〈見つける力が0〉は見つける力の半分点 N0 を無限大にして作る（旧は技術水準0）。
    /// ⚠壊し方＝門が SiteFound を読まない（常に1）→ ①が赤／判定で SiteFound を戻す → ④が赤。</summary>
    [Fact]
    public void 産地は見つかるまで産まない_見つかったら開いて失われない()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ジャワのキナ");
        Assert.False(p.SiteFound, "前提が崩れた（開始から開いている）");
        Assert.Equal(0.0, Discovery.Gate(r, p));
        // ペルー（アンデス）の樹皮は開始から産む（表に無い＝開始時の賦存）
        bool peru = false;
        foreach (var n in sim.Nations)
            foreach (var rg in n.World.Regions)
                if (rg.Name == "ペルー")
                    foreach (var q in rg.Producers)
                        if (q.GoodId == Catalog.CinchonaBarkId) { peru = true; Assert.True(q.SiteRow < 0); Assert.Equal(1.0, Discovery.Gate(rg, q)); }
        Assert.True(peru, "ペルーの樹皮が無い");
        // 財の解禁（作る門・需要の門）は「場所がまだ知られていない」を「財がまだ発見されていない」と取り違えない
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.CinchonaBarkId));

        double n00 = w.Coef.DiscoverySiteFindHalfPop;
        w.Coef.DiscoverySiteFindHalfPop = NoFindingPower;   // 見つける力 0（旧は w.TechLevel＝0）
        var t0 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.True(t0.Tech == 0 && t0.Transmit == 0 && t0.Need <= Discovery.TermMax, $"項が範囲の外（{t0}）");
        Discovery.Step(w, null, w.Coef);
        Assert.False(p.SiteFound, "技術力0で開いた");

        w.Coef.DiscoverySiteFindHalfPop = n00;   // 見つける力を戻す
        int g = Catalog.Sites[p.SiteRow].Good;
        var b = SiteParts(w, r, p);
        Assert.True(b.Cost > 0 && b.People > 0, $"前提が崩れた（掘る費用 {b.Cost}・住民 {b.People}）");
        SiteMarket(w, g, b.Cost * System.Math.Exp(3.0));
        var t1 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.True(t1.Tech > 0 && t1.Tech <= Discovery.TermMax, $"技術力が範囲の外（{t1}）");
        Assert.True(t1.Score >= Discovery.GateScore, $"技術力と樹皮の値段（掘る費用の e³ 倍）で届かない（{t1}）");
        Discovery.Step(w, null, w.Coef);
        Assert.True(p.SiteFound);
        Assert.Equal(1.0, Discovery.Gate(r, p));

        w.Coef.DiscoverySiteFindHalfPop = NoFindingPower;   // 見つける力を 0 へ戻す
        for (int i = 0; i < 3; i++) Discovery.Step(w, null, w.Coef);
        Assert.True(p.SiteFound, "開いた産地が閉じた");
        Assert.Equal(1.0, Discovery.Gate(r, p));
        w.Coef.DiscoverySiteFindHalfPop = n00;
    }

    /// <summary>★(171) 人の厚みは見つける力（技術力の側）＝鉱床は入植者（その国の中核民族の人数）・植林は住民：
    /// 同じ州の住民を全て中核民族でなくすと、鉱床（ルイジアナの硫黄）の技術力は0＝必要性（≦70）だけでは届かない・必要性は変わらない（人は必要性に入らない）／
    /// 植林（ジャワのキナ）の技術力は変わらない。旧（(148)〜(169)）＝人の厚みは必要性の側（入植者×財の値＝2026-09-28 作者が採用した提案③＝文は AI の整理）。
    /// ⚠壊し方＝鉱床の人の厚みを住民で数える → 鉱床の技術力が0にならない＝赤／技術力が人の厚みを掛けない → 赤。</summary>
    [Fact]
    public void 鉱床は入植者が居なければ見つけられない_植林は住民で見る()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        var b = SiteParts(w, r, p);
        SiteMarket(w, Catalog.Sites[p.SiteRow].Good, b.Cost * System.Math.Exp(3.0));   // 必要性を立てる（人が居なくても必要性は変わらないことを見る）
        var t0 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.True(t0.Tech > 0 && t0.Need > 0, $"前提が崩れた（入植者が居るのに技術力か必要性が0・{t0}）");
        foreach (var q in r.Pops) q.CoreEthnicShare = new Share(0);
        var t = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.Equal(0.0, t.Tech);
        Assert.Equal(t0.Need, t.Need);
        Discovery.Step(w, null, w.Coef);
        Assert.False(p.SiteFound, $"入植者0の鉱床が開いた（{t}）");

        var (wk, rk, pk) = Site(sim, "ジャワのキナ");
        double before = Discovery.SiteTerms(wk, rk, pk, wk.Coef).Tech;
        Assert.True(before > 0, "前提が崩れた（植林の技術力0）");
        foreach (var q in rk.Pops) q.CoreEthnicShare = new Share(0);
        Assert.Equal(before, Discovery.SiteTerms(wk, rk, pk, wk.Coef).Tech);
    }

    /// <summary>★(171) 産地の必要性は作者 2026-10-02 の一般則を産地に当てた AI の読み（推奨で進めた・作者の承認は未確認・費用は国の今のレシピ＝産地の富みを読まない＝候補2とは呼ばない＝(172) 反証役 vd4#3）＝ln（その財の平滑化した値段÷その国で今のレシピで掘る費用）を
    /// 産地の半分点で飽和（発見の表の製法の行と同じ形）・技術力（見つける力）＝70×N÷(N＋N0)（★(474) 技術水準の比は (415) で外れた・旧は 70×min(1, 技術水準÷難易度)×N÷(N＋N0)）。盤面＝ルイジアナの硫黄：
    /// ①硫黄の値段を掘る費用の2倍に置くと必要性＝70×ln2÷(ln2＋産地の半分点)（参照価格は読まない＝掘る費用は参照価格と1%以上違う）
    /// ②掘る費用の0.9倍なら0 ③国内に需要が無ければ（世界の値段が掘る費用の2倍でも）0 ④入植者の数を半分にしても必要性は1ビットも変わらず、技術力は 70×N÷(N＋N0) のとおり
    /// ⑤投入（農業労働力）の値段を2倍にすると掘る費用が上がり必要性が下がる ⑥判定（Step）は届けば開く＝呼び手も同じ値段の帳を読む。
    /// ⚠壊し方＝必要性を参照価格と比べる／人の厚み×値段÷参照価格（旧の形）→ ①が赤／人の厚みを必要性に掛ける → ④が赤／投入を読まない → ⑤が赤／需要の門を外す → ③が赤。</summary>
    [Fact]
    public void 産地の必要性はその財の値段とその国で今のレシピで掘る費用の比_人の厚みは見つける力()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        var c = w.Coef;
        int g = Catalog.Sites[p.SiteRow].Good;
        foreach (var q in r.Pops) q.CoreEthnicShare = new Share(1);
        var b = SiteParts(w, r, p);
        Assert.True(b.Cost > 0 && b.People > 0, $"前提が崩れた（掘る費用 {b.Cost}・入植者 {b.People}）");
        Assert.True(System.Math.Abs(b.Cost / Catalog.RefPrices[g] - 1.0) > 0.01, $"前提が崩れた（掘る費用が参照価格と同じ・{b.Cost}）");
        double half = c.DiscoveryNeedHalfSite;
        // ①
        SiteMarket(w, g, 2.0 * b.Cost);
        var t1 = SiteParts(w, r, p);
        Assert.Equal(2.0 * b.Cost, t1.V, 12);
        Assert.Equal(b.Cost, t1.Cost, 12);
        Assert.Equal(Discovery.TermMax * System.Math.Log(2.0) / (System.Math.Log(2.0) + half), t1.T.Need, 9);
        // ②
        SiteMarket(w, g, 0.9 * b.Cost);
        Assert.Equal(0.0, SiteParts(w, r, p).T.Need);
        // ③ 国内に需要が無い（市場が死んでいる）＝世界の値段（運賃と関税の無い市場）が費用の2倍でも0
        //   （盤面＝ジャワのキナ＝蘭は樹皮を作る門の開いた生産者を持たない＝手に入れる値段は世界の値段。硫黄は米の他の州が同じレシピで作る＝値段≦費用で門を見られない）
        var (wj, rj, pj) = Site(sim, "ジャワのキナ");
        int gj = Catalog.Sites[pj.SiteRow].Good;
        var bj = SiteParts(wj, rj, pj);
        Assert.True(bj.Cost > 0, $"前提が崩れた（植えて剥ぐ費用 {bj.Cost}）");
        foreach (var rr in wj.Regions) if (rr.State == RegionState.Active) rr.Markets[gj].LastDemand = 0;
        var px3 = (double[])wj.DiscoveryPrice!.Clone(); px3[gj] = 2.0 * bj.Cost;
        var t3 = SiteParts(wj, rj, pj, null, new Discovery.WorldView(null, null, Discovery.WorldMarket.Flat(px3)));
        Assert.True(t3.V > 1.5 * t3.Cost, $"前提が崩れた（需要の無い国で値段が費用を超えない＝需要の門を見られない・{t3.V}÷{t3.Cost}）");
        Assert.Equal(0.0, t3.T.Need);
        // ④
        SiteMarket(w, g, 2.0 * b.Cost);
        double n0 = c.DiscoverySiteFindHalfPop;
        Assert.Equal(Discovery.TermMax * t1.People / (t1.People + n0), t1.T.Tech, 9);
        foreach (var q in r.Pops) q.CoreEthnicShare = new Share(0.5);
        var t4 = SiteParts(w, r, p);
        Assert.Equal(0.5 * t1.People, t4.People, 9);
        Assert.Equal(t1.T.Need, t4.T.Need);
        Assert.Equal(Discovery.TermMax * t4.People / (t4.People + n0), t4.T.Tech, 9);
        foreach (var q in r.Pops) q.CoreEthnicShare = new Share(1);
        // ⑤ 投入の値段＝農業労働力の平滑化した値段（国内の市場を生かして2倍）
        Assert.Contains(p.Inputs, i => i.GoodId == Catalog.FarmLaborId);
        double fl = w.DiscoveryPrice![Catalog.FarmLaborId];
        Assert.True(fl > 0, $"前提が崩れた（農業労働力の値段 {fl}）");
        SiteMarket(w, Catalog.FarmLaborId, 2.0 * fl);
        var t5 = SiteParts(w, r, p);
        Assert.True(t5.Cost > b.Cost * 1.01 && t5.T.Need < t1.T.Need, $"投入の値段が掘る費用に入らない（費用 {b.Cost}→{t5.Cost}・必要性 {t1.T.Need}→{t5.T.Need}）");
        SiteMarket(w, Catalog.FarmLaborId, fl);
        // ⑥ 判定＝届けば開く（値段を掘る費用の e³ 倍・見つける力の人の半分点を入植者の 1/9 へ＝厚み 0.9）
        c.DiscoverySiteFindHalfPop = t1.People / 9.0;
        SiteMarket(w, g, b.Cost * System.Math.Exp(3.0));
        var t6 = SiteParts(w, r, p);
        Assert.True(t6.T.Score >= Discovery.GateScore, $"前提が崩れた（届かない・{t6.T}）");
        Discovery.Step(w, w.LastDiscoveryView, c);
        Assert.True(p.SiteFound, "判定が産地の2項で届いても開かない");
    }

    /// <summary>開いた鉱床の富鉱は開いた財の生産者だけ（検討 Q13 (c)・旧＝州の全ての鉱物の MineralDepletion）：同じ州の他の生産者の生産性は1ビットも変わらない・
    /// 開いた生産者は ×GoldRushEndowment・操業規模≥1（開口＝即産）・州の鉱脈在庫は BonanzaDepositYears×初期在庫÷DepositYearsAtBirth だけ増える。
    /// ⚠壊し方＝州の全ての鉱物に富鉱を掛ける → 他の生産者が変わる＝赤。</summary>
    [Fact]
    public void 開いた鉱床の富鉱は開いた財の生産者だけ()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        var before = r.Producers.Select(q => q.Productivity).ToArray();
        r.MineralDepositInitial = 1000; r.MineralDeposit = 400;
        var c = w.Coef;
        Discovery.OpenSite(w, r, p, c);
        Assert.True(p.SiteFound);
        for (int i = 0; i < r.Producers.Count; i++)
        {
            var q = r.Producers[i];
            if (ReferenceEquals(q, p)) Assert.Equal(before[i] * c.GoldRushEndowment, q.Productivity, 12);
            else Assert.Equal(before[i], q.Productivity);
        }
        Assert.True(p.Scale >= 1.0);
        Assert.Equal(400 + c.BonanzaDepositYears * 1000 / c.DepositYearsAtBirth, r.MineralDeposit, 9);
    }

    // ===== ★世界のゲート（2026-10-01 (150)）＝工場の蒸気動力・近代港湾＝「蒸気機関」の行・鉄道輸送＝「機関車」の行・冷蔵＝「冷蔵」の行（旧＝技術水準の時計と工業化度の傾斜）の番人 =====

    /// <summary>単独の仏を8四半期育てた盤面（直接 EconomyStep・Transport を1回呼ぶ番人の土台＝呼ぶ間に発見の判定は走らない）。</summary>
    private static Simulation Grown(ulong seed = 21)
    {
        var s = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent());
        s.Run(8);
        return s;
    }

    /// <summary>工場の蒸気動力は「蒸気機関」の発見が開き、技術水準では開かない（旧＝技術水準 SteamTechOnset 0.25 を越えれば発見と関係なく石炭を焚いた）。
    /// 双子の盤面（同じ種）＝①技術水準5・未発見 ②技術水準0・発見済み → 月の経済を1回：①はどの工場も石炭を焚かない（SteamFill＝0）・②は焚く。
    /// ⚠壊し方＝門を技術水準の時計へ戻す（①が焚く）・州へ国の解禁を読ませない（②が焚かない）→ 赤。</summary>
    [Fact]
    public void 世界のゲート_工場の蒸気動力は蒸気機関の発見が開き技術水準では開かない()
    {
        double Fill(double tech, bool known)
        {
            var s = Grown(); var w = s.World; var r = w.Region;
            w.Discovered[Discovery.SteamRow] = known;
            w.TechLevel = tech;
            foreach (var p in r.Producers) p.SteamFill = 0;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            double f = 0; foreach (var p in r.Producers) f += p.SteamFill;
            return f;
        }
        Assert.Equal(0.0, Fill(5.0, false));
        Assert.True(Fill(0.0, true) > 0, "蒸気機関を発見した国の工場が石炭を焚かない");
    }

    /// <summary>鉄道の容量は「機関車」の発見が開き、技術水準では開かない（旧＝技術水準の傾斜 RailTechOnset 0.35〜0.85）。
    /// 双子の盤面＝線路と機関車の在庫と投資家の貯蓄を潤沢にして輸送を1回：①技術水準5・未発見＝容量を積まない ②技術水準0・発見済み＝積む。
    /// ⚠壊し方＝門を技術水準の時計へ戻す → 赤。</summary>
    [Fact]
    public void 世界のゲート_鉄道の容量は機関車の発見が開き技術水準では開かない()
    {
        double Built(double tech, bool known)
        {
            var s = Grown(); var w = s.World; var c = w.Coef;
            w.Discovered[Discovery.LocomotiveRow] = known; w.TechLevel = tech;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
                foreach (int g in new[] { c.RailGoodId, c.LocoGoodId }) { r.Markets[g].Stock = 1e6; r.Markets[g].Price = Catalog.RefPrices[g]; }
                r.Pops[r.InvestorPopIndex].SetSavings(1e7);
            }
            double after = w.RailCapacity * (1 - c.TransportDecay);   // 減耗の後
            Transport.Step(w, c);
            return w.RailCapacity - after;
        }
        Assert.Equal(0.0, Built(5.0, false), 12);
        Assert.True(Built(0.0, true) > 0, "機関車を発見した国が鉄道を積まない");
    }

    /// <summary>近代港湾（蒸気の浚渫と埠頭）の拡張は「蒸気機関」の発見が開き、技術水準では開かない（旧＝技術水準 PortTechOnset 0.2）。
    /// 英1国の盤面＝1つの港を混雑させて港の段を1回（非公開＝反射）：①技術水準5・未発見＝港の容量が増えない ②技術水準0・発見済み＝増える。
    /// ⚠壊し方＝門を技術水準の閾値へ戻す → 赤。</summary>
    [Fact]
    public void 世界のゲート_近代港湾は蒸気機関の発見が開き技術水準では開かない()
    {
        double Grow(double tech, bool known)
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            foreach (var n in sim.Nations) n.TelemetryEnabled = false;
            sim.Diplomacy.Log.Enabled = false;
            sim.StepQuarter();
            var w = sim.Nations[0].World;
            w.Discovered[Discovery.SteamRow] = known; w.TechLevel = tech;
            var port = w.Regions.Find(r => r.State == RegionState.Active && r.IsPort && r.InvestorPopIndex >= 0)!;
            foreach (var r in w.Regions) r.LastTradeThrottle = 1.0;
            port.LastTradeThrottle = 0; w.FiscalFundingRatio = new Share(1.0);
            port.Pops[port.InvestorPopIndex].SetSavings(1e7);
            double after = port.PortCapacity * (1 - sim.Coef.PortDecay);
            typeof(MultiNationSim).GetMethod("StepPorts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(sim, null);
            return port.PortCapacity - after;
        }
        Assert.Equal(0.0, Grow(5.0, false), 9);
        Assert.True(Grow(0.0, true) > 0, "蒸気機関を発見した国が港を広げない");
    }

    /// <summary>冷蔵（生鮮の腐敗の低減）は「冷蔵」の発見が開く（旧＝工業化度の傾斜だけ＝1790年代に全ての国で効いた）。効きの大きさは立っている設備の規模の傾斜のまま。
    /// 双子の盤面＝工業化度5（傾斜1）・肉の在庫を置いて月の経済を1回：腐った量の比（発見済み÷未発見）＝1−RefrigerationEffect。
    /// ⚠壊し方＝発見を読まない → 未発見でも効く＝比が1−効きにならない＝赤。</summary>
    [Fact]
    public void 世界のゲート_冷蔵は冷蔵の発見が開く()
    {
        double Lost(bool known)
        {
            var s = Grown(); var w = s.World; var r = w.Region;
            w.Discovered[Discovery.ReeferRow] = known;
            r.Industrialization = 5.0;
            var m = r.Markets[Catalog.MeatId];
            m.Stock = 1000;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return m.SpoilLog[(m.SpoilIdx + 11) % 12];
        }
        double off = Lost(false), on = Lost(true);
        Assert.True(off > 0, "前提が崩れた（肉が腐らない）");
        Assert.Equal(1.0 - new Coefficients().RefrigerationEffect, on / off, 9);
    }

    // ===== ★制度と思想（2026-10-01 (152)・設計 §1A-6）＝財を持たない行・必要性は行ごとに読む量・伝わりは思想と制度の到達 =====

    /// <summary>制度と思想の行は財を持たない＝技術の押しは1（技術力＝70×技術水準÷難易度）・伝わりは思想と制度の到達（World.SpreadReach）を読み、接触（運び手の財の輸入）は読まない。
    /// ⚠壊し方＝伝わりに接触を読む・到達を読まない → 到達を半分点に置いても伝わりが35にならない＝赤。</summary>
    [Fact]
    public void 制度と思想の行は財を持たず伝わりは到達を読む()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        w.Discovered[Discovery.SocialismRow] = true;   // ★(621) 共産主義 ← 社会主義の辺：前提が既知の国で見る
        foreach (var r in w.Regions) foreach (var pp in r.Pops) pp.Literacy = 1.0;   // ★(621) 技術の比は本国の識字÷0.5（満点にそろえる）
        Assert.Equal(6, Discovery.SpreadRows.Length);
        foreach (int row in Discovery.SpreadRows)
        {
            var d = Catalog.Discoveries[row];
            Assert.Equal(Catalog.NoGood, d.Good);
            Assert.True(Discovery.IsSpread(d.Field), $"{d.Name}：制度でも思想でもない");
            int slot = Discovery.SpreadSlot(row);
            w.SpreadReach[slot] = 0;
            var contact = new double[Catalog.Discoveries.Length]; contact[row] = 1e6;
            var t0 = Discovery.Evaluate(w, new Discovery.WorldView(contact), w.Coef, includeKnown: true)[row];
            Assert.Equal(0.0, t0.Transmit);   // 接触は読まない
            double diff = Discovery.Difficulty(row, w.Coef);
            Assert.Equal(Discovery.TermMax, t0.Tech, 9);   // 技術の押しは1（★(613) 旗 TechThresholdOff〔(824) で焼き込み〕＝制度と思想の行の技術の比は 1。★(621)(627) 既定では本国の識字÷0.5 が上書きする＝上で識字を 1.0 にそろえたので満点）
            w.SpreadReach[slot] = w.Coef.DiscoveryContactHalf;
            Assert.Equal(Discovery.TermMax / 2, Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[row].Transmit, 9);
        }
        Assert.Equal(-1, Catalog.DiscoveryRowOf(Catalog.NoGood, ""));   // 財で探しても制度と思想の行は出てこない
    }

    /// <summary>制度と思想の必要性は行ごとに読む量（設計 §1A-4・§1A-6）＝特許は中核エリート・株式会社は州の最大工業化度・社会主義と共産主義は工場労働者の人数×闘争度÷人口・
    /// 帝国主義は植民地の州に住む人の割合・ファシズムは危機（敗戦か恐慌×左派の脅威）×人々の闘争度。半分点は分野で1本。盤面の量をテストの側で数えて一致させる。
    /// ⚠壊し方＝特許を最大工業化度で読む・社会主義を全ての人の闘争度で読む・ファシズムの危機を外す → 赤。</summary>
    [Fact]
    public void 制度と思想の必要性は行ごとの量を分野の半分点で読む()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        w.Discovered[Discovery.SocialismRow] = true;   // ★(621) 共産主義 ← 社会主義の辺：前提が既知の国で見る
        var c = w.Coef;
        double maxInd = 0, pop = 0, wm = 0, col = 0, mil = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            if (r.TotalIndustrialization > maxInd) maxInd = r.TotalIndustrialization;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier || !(p.Size > 0)) continue;
                pop += p.Size;
                if (p.Militancy > 0) { mil += p.Size * p.Militancy; if (p.ClassId == PopClass.Worker) wm += p.Size * p.Militancy; }
                if (r.Tier == RegionTier.Colonial) col += p.Size;
            }
        }
        // ファシズムの危機を立てる（敗戦1×左派の脅威1＝全ての人の思想を社会主義へ）
        w.DefeatShock = 1.0;
        foreach (var p in w.AllPops()) { System.Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)Ideology.Socialism] = 1.0; }
        Assert.Equal(1.0, Ideologies.FascismCrisis(w), 12);
        Assert.True(wm > 0 && col > 0 && maxInd > 0 && mil > wm, $"前提が崩れた（工場労働者の闘争 {wm}・植民地 {col}・工業化 {maxInd}）");
        double hI = c.DiscoveryNeedHalfInstitution, hD = c.DiscoveryNeedHalfIdea;
        double N(double s, double h) => Discovery.TermMax * s / (s + h);
        w.ScoreStanding = 50;   // ★(640) ファシズムの強度＝危機×人々の闘争度×国力の落ち込み（既定で点火）＝(50−40)÷50＝0.2
        var t = Discovery.Evaluate(w, new Discovery.WorldView(null, ScoreNow: 40.0), c, includeKnown: true);
        Assert.Equal(N(w.EliteCore, hI), t[Discovery.PatentRow].Need, 9);
        Assert.Equal(N(maxInd, hI), t[Discovery.JointStockRow].Need, 9);
        Assert.Equal(N(wm / pop, hD), t[Discovery.SocialismRow].Need, 9);
        Assert.Equal(N(wm / pop, hD), t[Discovery.CommunismRow].Need, 9);
        Assert.Equal(N(col / pop, hD), t[Discovery.ImperialismRow].Need, 9);
        Assert.Equal(N(mil / pop * 0.2, hD), t[Discovery.FascismRow].Need, 9);
        w.DefeatShock = 0;   // 危機が無ければファシズムの必要性は0（GDP の落ち込みが無い盤面）
        if (!(w.GdpDrawdown.Value > 0)) Assert.Equal(0.0, Discovery.Evaluate(w, null, c, includeKnown: true)[Discovery.FascismRow].Need);
    }

    /// <summary>思想の門は発見の表の思想の行の2値（旧＝世界の発展度の傾斜＝全ての国がほぼ同時に開いた・計器 (152)＝社会主義の門 0.5 が主要国の全てで1788〜93年）。
    /// 双子の盤面＝社会主義の行を立てる／立てない → 門 1／0・思想の更新の後の社会主義の支持＞0／＝0。
    /// ⚠壊し方＝門を発展度の傾斜へ戻す・門を読まずに支持を出す → 赤。</summary>
    [Fact]
    public void 思想の門は発見の2値_見つけるまで支持は0()
    {
        var w = Nation(Board(), "イギリス");   // 思想の更新は毎回はじめから＝同じ盤面で門だけを変えて2度通す
        double Share(bool known, out double gate)
        {
            w.Discovered[Discovery.SocialismRow] = known;
            gate = Ideologies.EraGates(w).Socialism;
            Ideologies.UpdateShares(w);
            double s = 0; foreach (var p in w.AllPops()) s += p.Size * p.IdeologyShares[(int)Ideology.Socialism];
            return s;
        }
        double off = Share(false, out double g0), on = Share(true, out double g1);
        Assert.Equal(0.0, g0); Assert.Equal(1.0, g1);
        Assert.Equal(0.0, off);
        Assert.True(on > 0, "社会主義を見つけた国に社会主義の支持が立たない");
    }

    /// <summary>制度は発見の2値を読むだけ（状態を持たない）＝特許・株式会社は表の行・鉄道は「機関車」の行（旧＝Institutions.Step の閾値の bool と州の倍率）。
    /// 効き＝特許は革新（GrowTech）を×PatentInnovationBonus・株式会社は工業化の投資（EconomyStep）を×JointStockInvestBonus＝双子の盤面で発見の有無だけを変える。
    /// ⚠壊し方＝特許を読まずに革新・株式会社を読まずに投資（所有国の値を読まない）→ 双子が同じ＝赤。</summary>
    [Fact]
    public void 制度は発見の2値を読む_特許は革新を株式会社は投資を速める()
    {
        var w0 = Grown().World;
        foreach (bool k in new[] { false, true })
        {
            w0.Discovered[Discovery.PatentRow] = k; w0.Discovered[Discovery.JointStockRow] = !k; w0.Discovered[Discovery.LocomotiveRow] = k;
            Assert.Equal(k, w0.PatentLaw); Assert.Equal(!k, w0.JointStock); Assert.Equal(k, w0.Railways);
        }
        double Tech(bool patent)
        {
            var w = Nation(Board(), "イギリス");
            w.Freedom = 1.0; w.EducationLaw = EducationLaw.Public;   // 革新は自由＋公教育の国だけ（FrontierCapable）
            w.Discovered[Discovery.PatentRow] = patent;
            double t0 = w.TechLevel;
            TechEducation.GrowTech(w, w.Coef);
            return w.TechLevel - t0;
        }
        double tOff = Tech(false), tOn = Tech(true);
        Assert.True(tOff > 0, "前提が崩れた（技術が伸びない）");
        Assert.True(tOn > tOff * 1.0001, $"特許を見つけた国の技術の伸びが速くならない（{tOff}→{tOn}）");
        double Ind(bool js)
        {
            var s = Grown(); var w = s.World; var r = w.Region;
            w.Discovered[Discovery.JointStockRow] = js;
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
            double i0 = r.Industrialization;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return r.Industrialization - i0;
        }
        double iOff = Ind(false), iOn = Ind(true);
        Assert.True(iOff > 0, $"前提が崩れた（工業化の投資が立たない {iOff}）");
        Assert.True(iOn > iOff * 1.0001, $"株式会社を見つけた国の工業化の投資が速くならない（{iOff}→{iOn}）");
    }

    /// <summary>思想と制度の伝播（MultiNationSim.StepDiffusion）＝制度は解禁済みの国が1で放射し、取引のある国の到達が積まれる・自国の種は自国の到達へ書き戻さない
    /// （2026-09-04 E-12＝書き戻すと国内で正帰還が閉じた）。盤面＝英だけが株式会社を見つけた世界で1四半期。
    /// ⚠壊し方＝制度の放射を0にする（呼び手＝StepDiffusion の種）・自国の種を到達へ書き戻す → 赤。</summary>
    [Fact]
    public void 制度の到達は見つけた国から取引で届き_自国へは書き戻さない()
    {
        var sim = Board();
        int js = Discovery.JointStockRow, slot = Discovery.SpreadSlot(js);
        foreach (var n in sim.Nations) { n.World.Discovered[js] = false; System.Array.Clear(n.World.SpreadReach); }
        var uk = Nation(sim, "イギリス");
        uk.Discovered[js] = true;
        Assert.Equal(1.0, Discovery.SpreadSource(uk, slot, Ideologies.National(uk)));
        sim.StepQuarter();
        Assert.Equal(1, sim.Nations.Count(n => n.World.Discovered[js]));   // 1四半期では誰も新たに見つけない（前提）
        Assert.Equal(0.0, uk.SpreadReach[slot]);   // 自国の種は書き戻さない
        int reached = sim.Nations.Count(n => n.World.SpreadReach[slot] > 0);
        Assert.True(reached > 5, $"英の株式会社が取引のある国へ届かない（届いた国 {reached}）");
    }

    // ===== ★作者裁定 Q12「平滑化した価格」（2026-10-01 (150)・(147) の逸脱を直した）の番人 =====

    /// <summary>平滑化した値段（World.DiscoveryPrice）は判定の段の頭（Discovery.AdvancePrice）だけが四半期に1歩進める＝ s ← s ＋ 率×（瞬間 − s）・率＝工場の採算の平滑を四半期へ（1−(1−月の率)³）。
    /// 画面と計器（Evaluate）と判定（Step）は進めずに読む＝差し替えに平滑化した値段を渡したのと同じ点数・瞬間の値段を渡すと違う点数（値段を跳ねさせた盤面）。
    /// ⚠壊し方＝AdvancePrice が平滑しない（瞬間の値で上書き）・画面か判定が進める → 赤。呼び手の側（共有層が四半期に1歩進める）は次の番人。</summary>
    [Fact]
    public void 平滑化した値段は判定の段の頭だけが1歩進め画面と判定は読むだけ()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス"); var c = w.Coef;
        Assert.NotNull(w.DiscoveryPrice);   // 最初の四半期の判定の段で瞬間の値から始まった
        var s0 = (double[])w.DiscoveryPrice!.Clone();
        Discovery.Evaluate(w, w.LastDiscoveryView, c);
        Discovery.Step(w, w.LastDiscoveryView, c);
        Assert.Equal(s0, w.DiscoveryPrice);   // 画面・計器・判定は進めない
        foreach (var r in w.Regions) if (r.State == RegionState.Active) r.Markets[Catalog.CoalId].Price *= 10;   // 石炭の値段の跳ね
        var x1 = Discovery.InstantRealPrices(w);
        Discovery.AdvancePrice(w, c);
        double a = Discovery.PriceSmoothingQ(c);
        Assert.Equal(1.0 - System.Math.Pow(1.0 - c.FactoryMarginSmoothing, 3), a, 12);
        // ★(165) 観測なし（死んだ市場＝NoPrice）の財は動かさない・一度も観測していない財は最初の観測で瞬間の値から（旧 (159)＝参照価格へ寄せた）
        int held = 0;
        for (int g = 0; g < s0.Length; g++)
        {
            double expect = !(x1[g] >= 0) ? s0[g] : s0[g] >= 0 ? s0[g] + a * (x1[g] - s0[g]) : x1[g];
            if (!(x1[g] >= 0)) held++;
            Assert.Equal(expect, w.DiscoveryPrice![g], 12);
        }
        Assert.True(held > 0, "前提が崩れた（死んだ市場の財が無い盤面）");
        // 読む値段の差し替え（計器だけ）＝既定は平滑化した値段・瞬間の値段を渡すと違う点数（灯油＝ガス灯が代替する物を割高にした盤面で比べる）。
        int h = Catalog.LampOilId;
        Assert.True(w.DiscoveryPrice![h] >= 0, "前提が崩れた（英の灯油の市場が死んでいる）");
        w.DiscoveryPrice![h] *= 3.0;
        var tS = Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true);
        var tP = Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true, realPrice: w.DiscoveryPrice);
        var inst = (double[])w.DiscoveryPrice.Clone();
        inst[h] = w.DiscoveryPrice[h] * 4.0;   // 差し替えの値段は灯油だけ跳ねる（ほかは平滑化した値段と同じ＝上の石炭の跳ねは混ぜない）
        var tI = Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true, realPrice: inst);
        for (int r = 0; r < tS.Length; r++) Assert.Equal(tP[r].Score, tS[r].Score);
        var roS = new (double Replaced, double Cost)[tS.Length]; var roI = new (double Replaced, double Cost)[tS.Length];
        Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true, ratioOut: roS); Discovery.Evaluate(w, w.LastDiscoveryView, c, includeKnown: true, realPrice: inst, ratioOut: roI);
        Assert.True(tI[Row("ガス灯")].Need > tS[Row("ガス灯")].Need, $"瞬間の値段と平滑化した値段で必要性が変わらない（盤面が値段を読んでいない・平滑 {roS[Row("ガス灯")]}・瞬間 {roI[Row("ガス灯")]}）");
    }

    /// <summary>呼び手の側の番人（(159)）＝多国の四半期末の判定の段は、全ての国の平滑化した値段を四半期に1歩進める（書き手は判定の段の頭の1か所）
    /// ＝四半期の後の平滑化した値段＝前の値＋率×（判定の時点の瞬間の値段−前の値）・判定の後に市場の Last* を書く処理は無い＝四半期の後の瞬間の値段が判定の時点の値。
    /// ⚠壊し方＝共有層が進めない（null のまま・凍る）・2回進める → 赤。</summary>
    [Fact]
    public void 判定の段は全ての国の平滑化した値段を四半期に1歩進める()
    {
        var sim = Board();
        double a = Discovery.PriceSmoothingQ(sim.Nations[0].World.Coef);
        var before = sim.Nations.Select(n => (double[])n.World.DiscoveryPrice!.Clone()).ToArray();
        sim.StepQuarter();
        int moved = 0;
        for (int i = 0; i < sim.Nations.Count && i < before.Length; i++)
        {
            var w = sim.Nations[i].World;
            var s1 = w.DiscoveryPrice;
            Assert.NotNull(s1);
            var x = Discovery.InstantRealPrices(w);
            for (int g = 0; g < s1!.Length; g++)
            {
                double b0 = before[i][g];
                double expect = !(x[g] >= 0) ? b0 : b0 >= 0 ? b0 + a * (x[g] - b0) : x[g];   // ★(165) 死んだ市場は動かさない・最初の観測は瞬間の値から
                Assert.Equal(expect, s1[g], 9);
            }
            if (!s1.SequenceEqual(before[i])) moved++;
        }
        Assert.True(moved > 0, "四半期を進めても平滑化した値段が1つも動かない");
    }

    /// <summary>呼び手の側の番人（単独の世界・(160)＝(159) の反証役の指摘・低）＝単独の世界（StepsDiplomacy）の四半期末の判定の段も、平滑化した値段を四半期に1歩進める
    /// （書き手は判定の段の頭の1か所）。多国の番人（上）は MultiNationSim の共有層だけを見る＝Simulation の呼び手を消しても緑だった。
    /// 盤面＝英の単独の世界：最初の四半期の判定の段で瞬間の値から始まる（null でない）→ 平滑化した値段を2倍に置いてもう1四半期
    /// ＝末尾の陸の輸送の値段（物価水準を読まない＝判定の後の物価水準の更新で動かない）が s ＋ 率×（瞬間 − s）にちょうど1歩。
    /// ⚠壊し方＝Simulation が AdvancePrice を呼ばない（null のまま＝判定が瞬間の値段を読む）・2回進める → 赤。</summary>
    [Fact]
    public void 単独の世界の判定の段も平滑化した値段を四半期に1歩進める()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; var c = w.Coef;
        Assert.True(w.StepsDiplomacy, "前提が崩れた（単独の世界が判定の段を通らない）");
        sim.StepQuarter();
        Assert.NotNull(w.DiscoveryPrice);   // 最初の判定の段で瞬間の値から始まった（呼び手が無ければ null のまま）
        int ng = Catalog.Goods.Length;
        Assert.Equal(ng + 1, w.DiscoveryPrice!.Length);
        var s0 = w.DiscoveryPrice!.Select(v => 2.0 * v).ToArray();
        w.DiscoveryPrice = (double[])s0.Clone();
        double a = Discovery.PriceSmoothingQ(c);
        sim.StepQuarter();
        double x = Discovery.InstantRealPrices(w)[ng];   // 陸の輸送の値段（判定の時点と同じ＝州の交易の潜在量と通した割合は四半期末に書かれない）
        Assert.True(System.Math.Abs(x - s0[ng]) > 1e-6, "前提が崩れた（1歩と2歩が見分けられない）");
        Assert.Equal(s0[ng] + a * (x - s0[ng]), w.DiscoveryPrice![ng], 12);
    }

    /// <summary>製法の行の判定も一過性の値段の跳ねでは開かない（作者裁定 Q12・(150) の番人の穴 M6＝製法の行の判定が瞬間の値段を読む変異が緑だった・(159)・(165) で比べる相手を今の費用へ）。
    /// 盤面＝英の人絹（★(474) 技術力は今の機構で作る＝時計 S＝1・自国の項 C＝0.8〔技術力 56〕）：平滑化した値段＝瞬間の値段・絹の値段＝人絹の今の費用のすぐ下（必要性0＝門に届かない）→ 瞬間の値段だけを、瞬間なら門に届き平滑なら届かない倍へ跳ねさせて
    /// 判定の段（頭で平滑を1歩・判定）を1回＝開かない（平滑化した値段は1歩だけ動く）。⚠壊し方＝判定が瞬間の値段を読む → 開く＝赤。</summary>
    [Fact]
    public void 製法の行の判定は一過性の値段の跳ねでは開かない()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス"); var c = w.Coef;
        int row = Row("人絹"), g = Catalog.SilkClothId;
        Assert.False(w.Discovered[row], "前提が崩れた（英が人絹を発見済み）");
        Assert.True(AliveIn(w, g), "前提が崩れた（英の絹織物の市場が死んでいる）");
        var view = DiscoveryPremise.OpenClock();   // ★(474) 旧は w.TechLevel＝難易度の2倍（旗0の盤面）
        Assert.True(DiscoveryPremise.OwnShare(w, row, 0.8, view), "前提が崩れた（英の絹織物の産業に高度労働力の雇用が無い＝自国の項 C を立てられない）");
        double pl = w.NominalOf(1.0);
        w.DiscoveryPrice = Discovery.InstantRealPrices(w);   // 平滑化した値段＝瞬間の値段
        var ro = new (double Replaced, double Cost)[Catalog.Discoveries.Length];
        Discovery.Evaluate(w, view, c, ratioOut: ro);
        double cost = ro[row].Cost;   // 人絹の今の費用（絹は人絹の投入でない＝絹の値段を動かしても変わらない）
        Assert.True(cost > 0, "前提が崩れた（人絹の費用が立たない）");
        foreach (var rg in w.Regions) if (rg.State == RegionState.Active) rg.Markets[g].Price = cost * pl * (1.0 - 1e-12);   // 瞬間の値段＝費用のすぐ下
        var inst0 = Discovery.InstantRealPrices(w);
        Assert.Equal(cost, inst0[g], 9);
        w.DiscoveryPrice = (double[])inst0.Clone();   // 平滑化した値段＝瞬間の値段
        var t0 = Discovery.Evaluate(w, view, c)[row];
        Assert.True(t0.Need == 0 && t0.Score < Discovery.GateScore, $"前提が崩れた（{t0}）");
        double need = Discovery.GateScore - t0.Tech, h = c.DiscoveryNeedHalfProcess;
        Assert.True(need > 0 && need < Discovery.TermMax, $"前提が崩れた（技術力 {t0.Tech:F1}）");
        double sOpen = h * need / (Discovery.TermMax - need);                       // 門に届く強度
        double a = Discovery.PriceSmoothingQ(c);
        double v = 1.0 + 0.5 * (System.Math.Exp(sOpen) - 1.0) / a;                    // 平滑なら届く倍の半分・瞬間なら ln(v) > sOpen
        Assert.True(System.Math.Log(v) > sOpen && System.Math.Log(1.0 + a * (v - 1.0)) < sOpen, "前提が崩れた（跳ねの倍）");
        foreach (var rg in w.Regions) if (rg.State == RegionState.Active) rg.Markets[g].Price *= v;   // 瞬間の値段だけ跳ねる
        var inst1 = Discovery.InstantRealPrices(w);
        Assert.True(Discovery.Evaluate(w, view, c, realPrice: inst1)[row].Score >= Discovery.GateScore, "前提が崩れた（瞬間の値段でも門に届かない）");
        Discovery.AdvancePrice(w, c); Discovery.Step(w, view, c);
        Assert.False(w.Discovered[row], "一過性の値段の跳ねで製法が開いた（判定が平滑化した値段を読んでいない）");
        Assert.Equal(inst0[g] + a * (inst1[g] - inst0[g]), w.DiscoveryPrice![g], 12);
    }

    /// <summary>判定は一過性の値段の跳ねでは開かない（作者裁定 Q12・(148) の申し送り＝値段の跳ねで必要性が飽和して樹皮と硫黄の産地が早く開いた）。
    /// 盤面＝ルイジアナの硫黄（★(171) 見つける力の人の半分点を入植者の 1/9＝厚み 0.9＝技術力 63）：平滑化した値段を掘る費用に置くと必要性0＝門に届かない・
    /// 瞬間の値段だけを門の2倍の強度へ跳ねさせて判定を1回＝開かない（平滑化した値段は1歩だけ動く）。⚠壊し方＝判定が瞬間の値段を読む → 開く＝赤。</summary>
    [Fact]
    public void 判定は一過性の値段の跳ねでは開かない()
    {
        // ★(388) 既定が DiscoveryPriceInitFromCost＝1 になった。この盤面の前提は〈平滑化した値段が最初の観測そのもの〉（掘る費用の投入の値段が瞬間と平滑で同じ）＝旧の始め方（0）に固定する。
        //   Q12 の主張（判定は平滑化した値段を読む）は始め方に依らない＝下の主張の行は変えない。
        var sim = Board(coef: new Coefficients { DiscoveryPriceInitFromCost = 0 });
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        var c = w.Coef; int g = Catalog.Sites[p.SiteRow].Good;
        foreach (var q in r.Pops) q.CoreEthnicShare = new Share(1);
        var b = SiteParts(w, r, p);
        Assert.True(b.Cost > 0 && b.People > 0, $"前提が崩れた（掘る費用 {b.Cost}・入植者 {b.People}）");
        c.DiscoverySiteFindHalfPop = b.People / 9.0;                                        // 見つける力＝70×0.9＝63
        SiteMarket(w, g, b.Cost);                                                           // 平滑化した値段＝掘る費用（強度0）
        var t1 = Discovery.SiteTerms(w, r, p, c);
        Assert.True(t1.Score < Discovery.GateScore && t1.Need == 0, $"前提が崩れた（値段が費用と同じでも開く・{t1}）");
        double half = c.DiscoveryNeedHalfSite;
        double need = Discovery.GateScore - t1.Tech;
        Assert.True(need > 0 && need < Discovery.TermMax, $"前提が崩れた（技術力 {t1.Tech:F1}）");
        double sOpen = half * need / (Discovery.TermMax - need);                            // 門に届く強度
        double v = b.Cost * System.Math.Exp(2.0 * sOpen);                                   // 瞬間の値段で門の2倍の強度になる値段
        double a = Discovery.PriceSmoothingQ(c);
        Assert.True(System.Math.Log((b.Cost + a * (v - b.Cost)) / b.Cost) < sOpen, "前提が崩れた（平滑でも届く跳ね）");
        double pl = w.NominalOf(1.0);
        foreach (var rr in w.Regions)
        {
            if (rr.State != RegionState.Active) continue;
            rr.Markets[g].Price = v * pl;                                                   // 生きた市場で瞬間の値段だけ跳ねる
        }
        Assert.True(Discovery.SiteTerms(w, r, p, c, Discovery.InstantRealPrices(w)).Score >= Discovery.GateScore, "前提が崩れた（瞬間の値段でも門に届かない）");
        Discovery.AdvancePrice(w, c); Discovery.Step(w, w.LastDiscoveryView, c);   // 判定の段（頭で平滑を1歩・判定は読むだけ）
        Assert.False(p.SiteFound, "一過性の値段の跳ねで産地が開いた（判定が平滑化した値段を読んでいない）");
        Assert.Equal(b.Cost + a * (v - b.Cost), w.DiscoveryPrice![g], 9);
    }
    // ===== ★(320) 流れG：平滑化した値段の最初の値（係数 DiscoveryPriceInitFromCost）の番人 =====

    /// <summary>自然価格（その国でその財を作る最も安い門の開いたレシピの費用）を、財の定義から手で出す（PriceBook を使わない別の出所）：
    /// 投入の無い財（労働力の人力・石炭）＝最初の門の開いたレシピの生産者の自己費用×自己費用の物価水準÷物価水準。</summary>
    private static double NaturalByHand(World w, int good, string recipe)
    {
        double plv = w.NominalOf(1.0), selfScale = w.CostPriceLevel / plv;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
                if (p.GoodId == good && p.RecipeName == recipe && Discovery.Gate(r, p) > 0) return p.MarginalCost * selfScale;
        }
        return double.NaN;
    }

    /// <summary>係数0（旧・★(388) 前の既定）では、履歴の無い国の最初の平滑化した値段は最初の観測の瞬間の値そのもの＝旧と同じ（恒等）。
    /// 係数が正なら、最初の値は自然価格（投入の無い財は手で出せる）から始まり最初の観測が他の四半期と同じ率 a で入る＝ s ＝ n ＋ a×(x − n)・n＝自己費用×自己費用の物価水準÷物価水準（労働力＝人力・石炭）。
    /// 関係で書く：n は (s − a×x)÷(1 − a) で逆算でき、手で出した自然価格と一致する・s は n と x の間にある。前提の確かめ＝自然価格が瞬間の値と違う国が十分あること（何もしない機構が緑にならない）。
    /// ⚠壊し方＝自然価格を使わず瞬間の値で始める・最初の観測を入れない（s＝n）・率を変える・世界の平均を先入観にする → 赤。</summary>
    [Fact]
    public void 自然価格から始めると最初の平滑化した値段が自然価格と同じ率の観測で閉じる()
    {
        // 係数 0＝旧と同じ（瞬間の値そのもの）。★(388) 既定が 1 になったので 0 を明示する
        var sim0 = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent(), new Coefficients { DiscoveryPriceInitFromCost = 0 });
        sim0.StepQuarter();
        int exact = 0;
        foreach (var n in sim0.Nations)
        {
            var w = n.World; var x = Discovery.InstantRealPrices(w); var s = w.DiscoveryPrice!;
            for (int g = 0; g < x.Length; g++) if (x[g] >= 0) { Assert.Equal(x[g], s[g], 12); exact++; }
        }
        Assert.True(exact > 1000, $"前提が崩れた（既定の盤面の観測が少ない {exact}）");

        // 係数が正
        var coef = new Coefficients { DiscoveryPriceInitFromCost = 1 };
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent(), coef);
        foreach (var n in sim.Nations) n.World.CostPriceLevel = 1.3;   // 自己費用の物価水準が物価水準と違う盤面（助走の無い盤面は 1 のまま＝掛け忘れが見えない）
        sim.StepQuarter();
        double a = Discovery.PriceSmoothingQ(coef);
        Assert.InRange(a, 0.01, 0.5);
        int differs = 0, checkedN = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var x = Discovery.InstantRealPrices(w); var s = w.DiscoveryPrice!;
            foreach (var (good, recipe) in new[] { (Catalog.LaborId, ""), (Catalog.CoalId, "") })
            {
                if (!(x[good] >= 0)) continue;
                double nat = NaturalByHand(w, good, recipe);
                if (double.IsNaN(nat)) continue;
                Assert.True(nat > 0);
                double back = (s[good] - a * x[good]) / (1.0 - a);   // 最初の値から逆算した自然価格
                Assert.True(System.Math.Abs(back - nat) <= 1e-9 * nat, $"{Instruments.NameOf(w)} {Catalog.Goods[good].Name}：逆算した自然価格 {back} が手で出した {nat} と違う");
                Assert.True(s[good] >= System.Math.Min(nat, x[good]) - 1e-12 && s[good] <= System.Math.Max(nat, x[good]) + 1e-12, "最初の値が自然価格と瞬間の値の間に無い");
                checkedN++;
                if (System.Math.Abs(s[good] - x[good]) > 1e-6) differs++;
            }
        }
        Assert.True(checkedN >= 50 && differs >= 50, $"前提が崩れた（確かめた国×財 {checkedN}・自然価格が瞬間の値と違う {differs}）");
    }

    // ===== ★(360) 流れG 伝わりの道（係数 DiscoveryCarrierDownstream）の番人 =====

    /// <summary>既定（0）の運び手の表は旧と同じ（蒸気機関の行＝蒸気機関の財だけ）。係数が正なら、各行の運び手の表の先頭は旧の表のまま（順序も）で、
    /// 足されるのは「旧の運び手を投入に持つ・取引できる・資本財（Producer 段）」だけを財の番号の昇順で＝1代だけ（足した財の先は辿らない）。ただし旧の運び手のうち使い道が全て発見の行に縛られた財（蒸気機関など）からだけ足す。
    /// 蒸気機関の行の足し分は、財の表から手で数えた機関車と汽船（蒸気機関の財 0.6 を投入に持つ最終財）と一致する。
    /// 前提の確かめ＝足される行が複数ある（何もしない機構が緑にならない）。⚠壊し方＝2代以上辿る・旧の運び手を落とす・取引できない財や資本財でない財を足す・足し分の順序を乱す → 赤。</summary>
    [Fact]
    public void 伝わりの道は運び手の表に設備を使う資本財を1代だけ足す()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        var worlds = sim.Nations.Select(n => n.World).ToList();
        var old = Discovery.CarriersByRow(worlds);
        var off = Discovery.CarriersByRow(worlds, false);
        for (int r = 0; r < old.Length; r++) Assert.Equal(old[r], off[r]);   // 既定の引数＝旧
        Assert.Equal([Catalog.SteamEngineId], old[Row("蒸気機関")]);
        var on = Discovery.CarriersByRow(worlds, true);
        Assert.Equal(old.Length, on.Length);
        int changed = 0;
        for (int r = 0; r < old.Length; r++)
        {
            Assert.Equal(old[r], on[r].Take(old[r].Length).ToArray());   // 旧の運び手が先頭にそのまま残る
            var extra = on[r].Skip(old[r].Length).ToArray();
            Assert.Equal(extra.OrderBy(g => g).ToArray(), extra);          // 足し分は番号の昇順
            Assert.Equal(extra.Length, extra.Distinct().Count());
            foreach (int g in extra)
            {
                Assert.DoesNotContain(g, old[r]);
                Assert.Equal(GoodTier.Producer, Catalog.Goods[g].Tier);
                Assert.False(Catalog.Goods[g].NonTradeable);
                // 財の表（別の出所）から：その財の既定のレシピの投入のどれかが旧の運び手
                Assert.Contains(Catalog.Goods[g].Inputs, i => old[r].Contains(i.GoodId));
            }
            if (extra.Length > 0) changed++;
        }
        Assert.True(changed >= 2, $"前提が崩れた（足し分のある行 {changed}）");
        // 蒸気機関の行：蒸気機関の財を投入に持つ最終財＝汽船と機関車（財の表から手で）。足した財（汽船・機関車）の先は辿らない＝足し分はこの 2 つだけ
        int[] byHand = [.. Catalog.Goods.Where(d => d.Tier == GoodTier.Producer && !d.NonTradeable && d.Inputs.Any(i => i.GoodId == Catalog.SteamEngineId)).Select(d => d.Id).OrderBy(g => g)];
        Assert.Contains(Catalog.SteamshipId, byHand); Assert.Contains(Catalog.LocomotiveId, byHand);
        Assert.Equal([Catalog.SteamEngineId, .. byHand], on[Row("蒸気機関")]);
        // 使い道が行に縛られない財（機械＝蒸気機関の財の既定のレシピは発見の行を持たない）は足さない＝機械を運び手にする行（石炭火力・水力発電ほか）は旧の表のまま
        Assert.True(Catalog.DiscoveryRowOf(Catalog.SteamEngineId, "") < 0, "前提が崩れた（蒸気機関の財の既定のレシピが行に縛られている）");
        Assert.Contains(Catalog.Goods[Catalog.SteamEngineId].Inputs, i => i.GoodId == Catalog.MachineryId);
        foreach (string nm in new[] { "石炭火力", "水力発電", "石油火力", "送電網", "蒸気タービン" }) Assert.Equal(old[Row(nm)], on[Row(nm)]);
        Assert.Equal(old[Row("転炉鋼")], on[Row("転炉鋼")]);   // 鋼（線路ほか行に縛られない使い道がある）
    }

    /// <summary>国境を越えた成約の記帳：蒸気機関を知る国（英）から、知らない国（日）が機関車を買うと、既定（0）では蒸気機関の行へ記帳されず（機関車の行そのものは英が未解禁なので記帳されない）、
    /// 係数が正なら蒸気機関の行へその額が記帳される。売り手がその行を知らない国なら記帳されない（運び手の述語は変えない）。
    /// ⚠壊し方＝運び手の表を引かず別の行へ記帳する・買い手が知っている行にも記帳する・係数が 0 でも足す → 赤。</summary>
    [Fact]
    public void 機関車を買うと蒸気機関の行の接触が係数が正のときだけ記帳される()
    {
        int steam = Row("蒸気機関");
        double Booked(double flag, string seller, string buyer, int good, out bool premise)
        {
            var coef = new Coefficients { DiscoveryCarrierDownstream = flag };
            var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent(), coef);
            int si = sim.Nations.FindIndex(n => Instruments.NameOf(n.World) == seller), bi = sim.Nations.FindIndex(n => Instruments.NameOf(n.World) == buyer);
            Assert.True(si >= 0 && bi >= 0);
            premise = Discovery.Known(sim.Nations[si].World.Discovered, steam) && !Discovery.Known(sim.Nations[bi].World.Discovered, steam);
            sim.Market.AccumulateDiscovery(sim.Nations, coef, si, bi, good, 10.0);
            return sim.Nations[bi].World.DiscoveryImportQ?[steam] ?? 0;
        }
        Assert.Equal(0, Booked(0, "イギリス", "日本", Catalog.LocomotiveId, out bool p0));
        Assert.Equal(10.0, Booked(1, "イギリス", "日本", Catalog.LocomotiveId, out bool p1), 12);
        Assert.Equal(10.0, Booked(1, "イギリス", "日本", Catalog.SteamshipId, out bool p2), 12);
        Assert.Equal(10.0, Booked(0, "イギリス", "日本", Catalog.SteamEngineId, out bool p3), 12);   // 旧の運び手（蒸気機関の財）は既定でも記帳される
        Assert.Equal(0, Booked(1, "日本", "イギリス", Catalog.LocomotiveId, out bool p4));          // 売り手が知らない・買い手が知っている
        Assert.True(p0 && p1 && p2 && p3, "前提が崩れた（英が蒸気機関を知り日本が知らない開幕でない）");
        Assert.False(p4);
    }
}
