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
    private static MultiNationSim Board(ulong seed = 7)
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent());
        sim.StepQuarter();
        return sim;
    }

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

    /// <summary>番人の側で組んだ「新しいレシピで作る費用」＝自己費用（数えるなら）×自己費用の物価水準÷物価水準＋Σ投入の量×差し替えの値段（投入が全て国内で生きている盤面だけ）。</summary>
    private static double CostByHand(World w, Producer rep, double[] px, bool selfCost = true)
    {
        double c = selfCost ? rep.MarginalCost * w.CostPriceLevel / w.NominalOf(1.0) : 0;
        foreach (var inp in rep.Inputs) if (inp.QtyPerOutput > 0) c += inp.QtyPerOutput * px[inp.GoodId];
        return c;
    }

    /// <summary>★作者 2026-10-02 の番人（(159)→(165)）＝「蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」＋比べる相手（作者「2でよい」＝代替する物の値段÷その発見が出す物の値段）
    /// ＝蒸気機関の行の必要性の強度＝ln（動力〔労働力の財〕の平滑化した値段÷蒸気の動力の費用〔蒸気機関の財と石炭の平滑化した値段×量＝石炭と機械・人の働きの自己費用は数えない〕）：
    /// 同じ盤面・同じ技術で、動力が蒸気の動力の費用と同じか安い国は必要性0＝自力では発見しない（技術力だけでは門に届かない）・2倍なら ln 2・
    /// 石炭を2倍にすると費用が上がって必要性が下がる（参照価格ではなく今の費用と比べる）・人件費（1人の所得）は読まない。
    /// ⚠壊し方＝参照価格と比べる・自己費用（人の働き）を数える・人件費を読む・技術力だけで解禁する（門を70へ）→ 赤。</summary>
    [Fact]
    public void 蒸気機関の行は石炭と機械の動力の費用と比べる_人の働きと人件費は読まない()
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
        var view = new Discovery.WorldView(null, null, own);   // 世界の平滑化した値段＝同じ値段（市場の死んだ投入を同じ値で読む）
        double cost = CostByHand(w, rep, own, selfCost: false);
        Assert.True(cost > 0 && rep.Inputs.Any(i => i.GoodId == Catalog.CoalId) && rep.Inputs.Any(i => i.GoodId == Catalog.SteamEngineId), "前提が崩れた（蒸気の動力のレシピ）");
        Discovery.Terms At(double overCost, double coalMul = 1.0)
        {
            var px = (double[])own.Clone(); px[Catalog.CoalId] *= coalMul;
            px[g] = CostByHand(w, rep, px, selfCost: false) * overCost;
            return Discovery.Evaluate(w, view with { WorldPrice = px }, w.Coef, includeKnown: true, realPrice: px)[steam];
        }
        var same = At(1.0); var cheap = At(0.5); var dear = At(2.0);
        Assert.Equal(0.0, same.Need);
        Assert.Equal(0.0, cheap.Need);
        Assert.True(cheap.Score < Discovery.GateScore, $"動力が安い国が技術力だけで届く（{cheap.Score:F1}）");
        Assert.Equal(Discovery.TermMax * System.Math.Log(2.0) / (System.Math.Log(2.0) + w.Coef.DiscoveryNeedHalfProcess), dear.Need, 9);   // 強度＝ln(動力÷石炭と機械の費用)
        // 今の費用と比べる＝動力の値段は同じで石炭だけ2倍＝費用が上がり必要性が下がる
        var px2 = (double[])own.Clone(); px2[g] = 2.0 * cost;
        var pxC = (double[])px2.Clone(); pxC[Catalog.CoalId] *= 2.0;
        double needBase = Discovery.Evaluate(w, view with { WorldPrice = px2 }, w.Coef, includeKnown: true, realPrice: px2)[steam].Need, needCoal = Discovery.Evaluate(w, view with { WorldPrice = pxC }, w.Coef, includeKnown: true, realPrice: pxC)[steam].Need;
        Assert.Equal(Discovery.TermMax * System.Math.Log(2.0) / (System.Math.Log(2.0) + w.Coef.DiscoveryNeedHalfProcess), needBase, 9);
        double s2 = System.Math.Log(2.0 * cost / CostByHand(w, rep, pxC, selfCost: false));
        Assert.Equal(s2 > 0 ? Discovery.TermMax * s2 / (s2 + w.Coef.DiscoveryNeedHalfProcess) : 0, needCoal, 9);
        Assert.True(needCoal < needBase, "石炭が高くなっても必要性が下がらない（今の費用と比べていない）");
        Assert.True(dear.Tech == cheap.Tech, "動力の値段で技術力が変わった");
        // 人件費（働き手の所得）は読まない＝所得を10倍にしても必要性は同じ
        foreach (var rg in w.Regions) foreach (var p in rg.Pops) p.Earned *= 10;
        Assert.Equal(dear.Need, At(2.0).Need);
        // 判定の1本（Step）も同じ＝安い国は解禁しない・高い国は解禁する（判定は平滑化した値段を読む＝その値を置く）
        var keep = w.DiscoveryPrice!;
        var pxCheap = (double[])own.Clone(); pxCheap[g] = 0.5 * cost;
        w.DiscoveryPrice = pxCheap;
        Discovery.Step(w, view with { WorldPrice = pxCheap }, w.Coef);
        Assert.False(w.Discovered[steam]);
        if (dear.Score >= Discovery.GateScore)
        {
            var pxDear = (double[])own.Clone(); pxDear[g] = 2.0 * cost;
            w.DiscoveryPrice = pxDear;
            Discovery.Step(w, view with { WorldPrice = pxDear }, w.Coef);
            Assert.True(w.Discovered[steam]);
        }
        w.DiscoveryPrice = keep;
    }

    /// <summary>伝わりだけでは届かない・技術力と揃えば届く（設計 §1A-2 の性質）＝必要性の無い行（合成ゴム＝ゴムをまだ作れない国の製法は必要性0）で、
    /// ①技術水準0・接触は最大（伝わり≈70）→ 届かない ②技術水準を難易度の2倍（技術力 70×押し）＋同じ接触 → 押しが半分以上の国は届く（そういう国が1つは在る）。
    /// ⚠壊し方＝門を70へ（伝わりだけで届く）→ ①が赤。</summary>
    [Fact]
    public void 伝わりだけでは届かず技術力と揃えば届く()
    {
        var sim = Board();
        int row = Row("合成ゴム");
        var contact = new double[Catalog.Discoveries.Length];
        contact[row] = 1e6;   // 接触は最大（伝わり→70）
        var view = new Discovery.WorldView(contact);
        int tried = 0, reached = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Discovered[row] || !(Discovery.Evaluate(w, null, w.Coef)[row].Need == 0)) continue;
            tried++;
            w.TechLevel = 0;
            var only = Discovery.Evaluate(w, view, w.Coef)[row];
            Assert.True(only.Transmit > 69.9 && only.Tech == 0 && only.Need == 0, $"前提が崩れた（{only}）");
            Discovery.Step(w, view, w.Coef);
            Assert.False(w.Discovered[row], $"{w.SelfPower?.Name}：伝わりだけで解禁した");
            w.TechLevel = Discovery.Difficulty(row, w.Coef) * 2;
            var both = Discovery.Evaluate(w, view, w.Coef)[row];
            if (!(both.Score >= Discovery.GateScore)) continue;
            reached++;
            Discovery.Step(w, view, w.Coef);
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
        w.TechLevel = 0;
        Assert.True(Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[row].Score < Discovery.GateScore, "前提が崩れた");
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
    /// ③機関車＝陸の輸送の値段÷鉄道の輸送の費用（清算の陸路の費用の鉄道÷馬・内陸の州の潜在量で重み付け）④代替する物の需要が国内に無いと0
    /// ⑤陸の輸送の値段＝(1÷通した割合)^(1/2ε)・通した割合0 は財の値段の天井（PriceCeilFactor）。
    /// ⚠壊し方＝参照価格と比べる（(159) の形）・代替する物の欄を読まない・換算の比を掛けない・投入の値段を読まない・機関車の比べる相手を1にする・需要の無い財を読む → 赤。
    /// ⚠(167) 反証役の指摘で名前を直した（旧「…_参照価格は読まない」）＝製法・動力・機関車は参照価格を読まないが、新製品は換算の比（参照価格の比）を掛ける
    ///   ＝強度＝ln((代替する物の値段÷その参照価格)÷(新しいレシピの費用÷新しい財の参照価格))＝新製品の必要性の0点は参照価格の比が決める（作者への確認②）。</summary>
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
        double[] Needs(double[] px) => Discovery.Evaluate(w, new Discovery.WorldView(null, null, px), w.Coef, includeKnown: true, realPrice: px).Select(x => x.Need).ToArray();
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
        var dearIn = (double[])basePx.Clone(); dearIn[gs] = 2.0 * cS; dearIn[Catalog.LumberId] *= 3.0;   // 投入（製材）が高い＝費用が上がる
        Assert.True(repS.Inputs.Any(i => i.GoodId == Catalog.LumberId), "前提が崩れた（人絹の投入に製材が無い）");
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
        // ③機関車＝陸の輸送の値段÷鉄道の輸送の費用（内陸の州の潜在量で重み付けた 鉄道÷馬 の陸路の費用）
        int loco = Row("機関車");
        Assert.Equal(Catalog.LandFreight, Catalog.Discoveries[loco].Replaces);
        double e = 1.0 / (2.0 * w.Coef.PriceElasticity);
        int inland = 0; double pot = 0, sum = 0, f = w.Coef.RailCostFraction;
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active || rg.IsPort) continue;
            inland++; rg.LastTradePotential = 1.0 + inland; rg.LastTradeThrottle = 0.25;   // 内陸の州の潜在量と律速率を置く（通した割合 0.25）
            int ti = (int)rg.Terrain;
            bool water = (rg.Terrain == Terrain.WaterMajor && w.Coef.TerrainWaterMajorCost > 0) || (rg.Terrain == Terrain.WaterMinor && w.Coef.TerrainWaterMinorCost > 0);
            double kk = water ? f : f * w.Coef.TerrainRailCost[ti] / w.Coef.TerrainHorseCost[ti];
            pot += rg.LastTradePotential; sum += rg.LastTradePotential * kk;
        }
        Assert.True(inland > 0, "前提が崩れた（英に内陸の州が無い）");
        double rail = sum / pot;
        Assert.True(rail > 0 && rail < 1, $"前提が崩れた（鉄道の輸送の費用 {rail}）");
        var pl = (double[])basePx.Clone(); pl[ng] = 2.0 * rail;
        Assert.Equal(NeedOf(loco, System.Math.Log(2.0)), Needs(pl)[loco], 9);
        pl[ng] = rail * 0.5;   // 鉄道の方が高い＝不足なし
        Assert.Equal(0.0, Needs(pl)[loco]);
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

    /// <summary>★(165) その国に市場の値段が無い財（作らず輸入もしない＝今の四半期に国内の市場が死んでいる）の読み方（理由で決めた・推奨で進めた）＝手に入れる安い方
    /// ＝世界の平滑化した値段（取引できる財＝輸入すれば払う値段）とこの国で門の開いたレシピで作る費用の小さい方・どちらも無ければ費用が立たない＝0：
    /// 盤面＝英の人絹（投入の製材の市場を英で殺す）→ ①世界の値段が作る費用より安ければ世界の値段で費用を組む ②高ければ英の製材のレシピで作る費用
    /// ③死んだ市場の凍った値段は読まない ④取引できない投入（電力＝電動機）は世界の値段では読まない。
    /// ⚠壊し方＝死んだ市場の凍った値段を読む・世界の値段を読まない・作る費用を読まない・取引できない財を世界の値段で読む → 赤。</summary>
    [Fact]
    public void 市場の値段が無い財は世界の値段か作る費用の安い方で読む()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        int ng = Catalog.Goods.Length, silk = Row("人絹"), gs = Catalog.SilkClothId, gl = Catalog.LumberId;
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
        double NeedAt(double? worldLumber)
        {
            var p = (double[])px.Clone(); p[gl] = 1e6;   // 死んだ市場の凍った値段（読まれない）
            double[]? wp = null;
            if (worldLumber is { } wl) { wp = (double[])px.Clone(); wp[gl] = wl; }
            return Discovery.Evaluate(w, new Discovery.WorldView(null, null, wp), w.Coef, includeKnown: true, realPrice: p)[silk].Need;
        }
        double Expect(double lumberPrice)
        {
            double c = cS - qL * px[gl] + qL * lumberPrice;
            double s = System.Math.Log(4.0 * cS / c);
            return s > 0 ? Discovery.TermMax * s / (s + w.Coef.DiscoveryNeedHalfProcess) : 0;
        }
        Assert.Equal(Expect(make), NeedAt(null), 9);              // 世界の値段が無い＝作る費用
        Assert.Equal(Expect(0.5 * make), NeedAt(0.5 * make), 9);  // 世界の値段が安い＝世界の値段
        Assert.Equal(Expect(make), NeedAt(10 * make), 9);         // 世界の値段が高い＝作る費用
        // ④取引できない投入（電力＝電動機）は世界の値段では読まない＝英に電力の市場も門の開いた電力のレシピも無ければ費用が立たない＝0
        int motor = Row("電動機");
        Assert.True(Catalog.Goods[Catalog.ElectricityId].NonTradeable, "前提が崩れた（電力は取引できない財）");
        Assert.True(!AliveIn(w, Catalog.ElectricityId) && !w.Regions.Any(rg => rg.Producers.Any(p => p.GoodId == Catalog.ElectricityId && Discovery.Gate(rg, p) > 0)),
            "前提が崩れた（開幕の英に電力がある）");
        var p2 = (double[])px.Clone(); p2[Catalog.LaborId] = 100.0;          // 動力がどれほど高くても
        var wp2 = (double[])px.Clone(); wp2[Catalog.ElectricityId] = 1e-6;   // 世界の電力がどれほど安くても
        Assert.Equal(0.0, Discovery.Evaluate(w, new Discovery.WorldView(null, null, wp2), w.Coef, includeKnown: true, realPrice: p2)[motor].Need);
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
        double NeedWith(double[]? wp) => Discovery.Evaluate(w, new Discovery.WorldView(null, null, wp), w.Coef, includeKnown: true, realPrice: px)[silk].Need;
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

    /// <summary>★(165) 世界の平滑化した値段＝その四半期に国内の市場が生きている国（瞬間の値段の観測がある国）の平滑化した値段を、その国の需要で重み付けた平均・どの国にも市場が無い財は観測なし。
    /// ⚠壊し方＝市場の死んだ国の値段を入れる・需要で重み付けない → 赤。</summary>
    [Fact]
    public void 世界の平滑化した値段は市場が生きている国の需要で重み付けた平均()
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
        var wp = Discovery.WorldPrices([a, b], [(xa, da), (xb, db)]);
        Assert.Equal((1.0 * 2.0 + 3.0 * 5.0) / 4.0, wp[0], 12);
        Assert.Equal(2.0, wp[7], 12);
        Assert.Equal(Discovery.NoPrice, wp[9]);
    }

    /// <summary>呼び手の側の番人（(165)）＝多国の判定の段は世界の平滑化した値段を判定へ渡す（画面と計器も同じ値＝LastDiscoveryView）＝四半期の後の値は全ての国の平滑化した値段と瞬間の値段から求めた値と同じ。
    /// ⚠壊し方＝共有層が世界の値段を渡さない → 赤。</summary>
    [Fact]
    public void 判定の段は世界の平滑化した値段を判定へ渡す()
    {
        var sim = Board();
        sim.StepQuarter();
        var view = sim.Nations[0].World.LastDiscoveryView;
        Assert.NotNull(view?.WorldPrice);
        var worlds = sim.Nations.Select(n => n.World).ToArray();
        var obs = worlds.Select(w =>
        {
            var x = Discovery.InstantRealPrices(w);
            var dem = new double[Catalog.Goods.Length];
            foreach (var rg in w.Regions) if (rg.State == RegionState.Active) for (int g = 0; g < dem.Length; g++) dem[g] += rg.Markets[g].LastDemand;
            return (x, dem);
        }).ToArray();
        var expect = Discovery.WorldPrices(worlds, obs);
        Assert.Equal(expect.Length, view!.WorldPrice!.Length);
        for (int g = 0; g < expect.Length; g++) Assert.Equal(expect[g], view.WorldPrice[g], 9);
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
            var t = Discovery.Evaluate(n.World, null, n.World.Coef, includeKnown: true);
            if (all.All(r => t[r].Tech > 0)) { w = n.World; break; }   // 6行とも代表の生産者が居る国
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

    /// <summary>見つかるまで産まない・見つかったら開く・失われない（2値）＋キナはアンデスだけが開始から（(129) の申し送り）：
    /// ①アジアのキナ（バタヴィア）の生産者は門0・ペルーの樹皮は門1 ②技術水準0では入植者が厚くても届かない（技術力0＋必要性≦70）
    /// ③技術水準を難易度へ上げると届いて開く（門1）④技術水準を0へ戻して判定を重ねても開いたまま。
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

        double tech0 = w.TechLevel;
        w.TechLevel = 0;
        var t0 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.True(t0.Tech == 0 && t0.Transmit == 0 && t0.Need <= Discovery.TermMax, $"項が範囲の外（{t0}）");
        Discovery.Step(w, null, w.Coef);
        Assert.False(p.SiteFound, "技術力0で開いた");

        w.TechLevel = Discovery.DifficultyOf(Catalog.Sites[p.SiteRow].Decade, w.Coef) * 2;
        var t1 = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.Equal(Discovery.TermMax, t1.Tech);
        Assert.True(t1.Score >= Discovery.GateScore, $"技術力70でも住民×樹皮の値で届かない（{t1}）");
        Discovery.Step(w, null, w.Coef);
        Assert.True(p.SiteFound);
        Assert.Equal(1.0, Discovery.Gate(r, p));

        w.TechLevel = 0;
        for (int i = 0; i < 3; i++) Discovery.Step(w, null, w.Coef);
        Assert.True(p.SiteFound, "開いた産地が閉じた");
        Assert.Equal(1.0, Discovery.Gate(r, p));
        w.TechLevel = tech0;
    }

    /// <summary>鉱床の必要性は入植者（その国の中核民族の人数）×財の値（設計 §9-1 の提案③・2026-09-28 作者が採用した提案＝文は AI の整理）・植林は住民：
    /// 同じ州の住民を全て中核民族でなくすと、鉱床（ルイジアナの硫黄）の必要性は0＝技術力70だけでは届かない／植林（ジャワのキナ）の必要性は変わらない。
    /// ⚠壊し方＝鉱床の人の厚みを住民で数える → 鉱床の必要性が0にならない＝赤。</summary>
    [Fact]
    public void 鉱床は入植者が居なければ見つからない_植林は住民で見る()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        w.TechLevel = Discovery.DifficultyOf(Catalog.Sites[p.SiteRow].Decade, w.Coef) * 2;
        Assert.True(Discovery.SiteTerms(w, r, p, w.Coef).Need > 0, "前提が崩れた（入植者が居るのに必要性0）");
        foreach (var q in r.Pops) q.CoreEthnicShare = 0;
        var t = Discovery.SiteTerms(w, r, p, w.Coef);
        Assert.Equal(0.0, t.Need);
        Discovery.Step(w, null, w.Coef);
        Assert.False(p.SiteFound, $"入植者0の鉱床が開いた（{t}）");

        var (wk, rk, pk) = Site(sim, "ジャワのキナ");
        double before = Discovery.SiteTerms(wk, rk, pk, wk.Coef).Need;
        Assert.True(before > 0, "前提が崩れた（植林の必要性0）");
        foreach (var q in rk.Pops) q.CoreEthnicShare = 0;
        Assert.Equal(before, Discovery.SiteTerms(wk, rk, pk, wk.Coef).Need);
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
            w.TechLevel = tech; r.TechLevel = tech;
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
            port.LastTradeThrottle = 0; w.FiscalFundingRatio = 1.0;
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
            Assert.Equal(Discovery.TermMax * (diff > 0 ? System.Math.Min(1, w.TechLevel / diff) : 1), t0.Tech, 9);   // 技術の押しは1
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
        var t = Discovery.Evaluate(w, null, c, includeKnown: true);
        Assert.Equal(N(w.EliteCore, hI), t[Discovery.PatentRow].Need, 9);
        Assert.Equal(N(maxInd, hI), t[Discovery.JointStockRow].Need, 9);
        Assert.Equal(N(wm / pop, hD), t[Discovery.SocialismRow].Need, 9);
        Assert.Equal(N(wm / pop, hD), t[Discovery.CommunismRow].Need, 9);
        Assert.Equal(N(col / pop, hD), t[Discovery.ImperialismRow].Need, 9);
        Assert.Equal(N(mil / pop, hD), t[Discovery.FascismRow].Need, 9);
        w.DefeatShock = 0;   // 危機が無ければファシズムの必要性は0（GDP の落ち込みが無い盤面）
        if (!(w.GdpDrawdown > 0)) Assert.Equal(0.0, Discovery.Evaluate(w, null, c, includeKnown: true)[Discovery.FascismRow].Need);
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
    /// 盤面＝英の人絹（技術水準＝難易度の2倍）：平滑化した値段＝瞬間の値段・絹の値段＝人絹の今の費用のすぐ下（必要性0＝門に届かない）→ 瞬間の値段だけを、瞬間なら門に届き平滑なら届かない倍へ跳ねさせて
    /// 判定の段（頭で平滑を1歩・判定）を1回＝開かない（平滑化した値段は1歩だけ動く）。⚠壊し方＝判定が瞬間の値段を読む → 開く＝赤。</summary>
    [Fact]
    public void 製法の行の判定は一過性の値段の跳ねでは開かない()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス"); var c = w.Coef;
        int row = Row("人絹"), g = Catalog.SilkClothId;
        Assert.False(w.Discovered[row], "前提が崩れた（英が人絹を発見済み）");
        Assert.True(AliveIn(w, g), "前提が崩れた（英の絹織物の市場が死んでいる）");
        w.TechLevel = Discovery.Difficulty(row, c) * 2;
        double pl = w.NominalOf(1.0);
        w.DiscoveryPrice = Discovery.InstantRealPrices(w);   // 平滑化した値段＝瞬間の値段
        var ro = new (double Replaced, double Cost)[Catalog.Discoveries.Length];
        Discovery.Evaluate(w, null, c, ratioOut: ro);
        double cost = ro[row].Cost;   // 人絹の今の費用（絹は人絹の投入でない＝絹の値段を動かしても変わらない）
        Assert.True(cost > 0, "前提が崩れた（人絹の費用が立たない）");
        foreach (var rg in w.Regions) if (rg.State == RegionState.Active) rg.Markets[g].Price = cost * pl * (1.0 - 1e-12);   // 瞬間の値段＝費用のすぐ下
        var inst0 = Discovery.InstantRealPrices(w);
        Assert.Equal(cost, inst0[g], 9);
        w.DiscoveryPrice = (double[])inst0.Clone();   // 平滑化した値段＝瞬間の値段
        var t0 = Discovery.Evaluate(w, null, c)[row];
        Assert.True(t0.Need == 0 && t0.Score < Discovery.GateScore, $"前提が崩れた（{t0}）");
        double need = Discovery.GateScore - t0.Tech, h = c.DiscoveryNeedHalfProcess;
        Assert.True(need > 0 && need < Discovery.TermMax, $"前提が崩れた（技術力 {t0.Tech:F1}）");
        double sOpen = h * need / (Discovery.TermMax - need);                       // 門に届く強度
        double a = Discovery.PriceSmoothingQ(c);
        double v = 1.0 + 0.5 * (System.Math.Exp(sOpen) - 1.0) / a;                    // 平滑なら届く倍の半分・瞬間なら ln(v) > sOpen
        Assert.True(System.Math.Log(v) > sOpen && System.Math.Log(1.0 + a * (v - 1.0)) < sOpen, "前提が崩れた（跳ねの倍）");
        foreach (var rg in w.Regions) if (rg.State == RegionState.Active) rg.Markets[g].Price *= v;   // 瞬間の値段だけ跳ねる
        var inst1 = Discovery.InstantRealPrices(w);
        Assert.True(Discovery.Evaluate(w, null, c, realPrice: inst1)[row].Score >= Discovery.GateScore, "前提が崩れた（瞬間の値段でも門に届かない）");
        Discovery.AdvancePrice(w, c); Discovery.Step(w, null, c);
        Assert.False(w.Discovered[row], "一過性の値段の跳ねで製法が開いた（判定が平滑化した値段を読んでいない）");
        Assert.Equal(inst0[g] + a * (inst1[g] - inst0[g]), w.DiscoveryPrice![g], 12);
    }

    /// <summary>判定は一過性の値段の跳ねでは開かない（作者裁定 Q12・(148) の申し送り＝値段の跳ねで必要性が飽和して樹皮と硫黄の産地が早く開いた）。
    /// 盤面＝ルイジアナの硫黄（技術力70・入植者を薄く）：平滑化した値段を参照価格に置くと門に届かない・瞬間の値段だけを門の2倍の強度へ跳ねさせて判定を1回＝開かない
    /// （平滑化した値段は1歩だけ動く）。⚠壊し方＝判定が瞬間の値段を読む → 開く＝赤。</summary>
    [Fact]
    public void 判定は一過性の値段の跳ねでは開かない()
    {
        var sim = Board();
        var (w, r, p) = Site(sim, "ルイジアナの硫黄");
        var c = w.Coef; int g = Catalog.Sites[p.SiteRow].Good; double rp = Catalog.RefPrices[g];
        w.TechLevel = Discovery.DifficultyOf(Catalog.Sites[p.SiteRow].Decade, c) * 2;   // 技術力70
        foreach (var q in r.Pops) q.CoreEthnicShare = 0.02;                               // 入植者を薄く
        Assert.NotNull(w.DiscoveryPrice);
        w.DiscoveryPrice![g] = rp;                                                          // 平滑化した値段＝参照価格（実質の値段1）
        var t1 = Discovery.SiteTerms(w, r, p, c);
        Assert.True(t1.Score < Discovery.GateScore, $"前提が崩れた（値段が普通でも開く・{t1.Score:F1}）");
        double half = c.DiscoveryNeedHalfSite;
        double thick = half * t1.Need / (Discovery.TermMax - t1.Need);                      // 実質の値段1のときの強度＝人の厚み
        double need = Discovery.GateScore - t1.Tech;
        double sOpen = half * need / (Discovery.TermMax - need);                            // 門に届く強度
        double v = 2.0 * sOpen / thick;                                                     // 瞬間の値段で門の2倍の強度になる実質の値段（参照価格の倍）
        double a = Discovery.PriceSmoothingQ(c);
        Assert.True(thick * (1 + a * (v - 1)) < sOpen, "前提が崩れた（平滑でも届く跳ね）");
        double pl = w.NominalOf(1.0);
        foreach (var rr in w.Regions)
        {
            if (rr.State != RegionState.Active) continue;
            var m = rr.Markets[g]; m.Price = v * rp * pl; m.LastDemand = 1; m.LastProduced = 1;   // 生きた市場で瞬間の値段だけ跳ねる
        }
        Assert.True(Discovery.SiteTerms(w, r, p, c, Discovery.InstantRealPrices(w)).Score >= Discovery.GateScore, "前提が崩れた（瞬間の値段でも門に届かない）");
        Discovery.AdvancePrice(w, c); Discovery.Step(w, w.LastDiscoveryView, c);   // 判定の段（頭で平滑を1歩・判定は読むだけ）
        Assert.False(p.SiteFound, "一過性の値段の跳ねで産地が開いた（判定が平滑化した値段を読んでいない）");
        Assert.Equal(rp + a * (v * rp - rp), w.DiscoveryPrice![g], 9);
    }
}
